using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PennyAssistantController : MonoBehaviour
{
    public System.Action onFeedbackDismissed;
    
    public UIDocument uiDocument;
    
    [Header("Animation Settings")]
    public List<Texture2D> frames;
    public float frameRate = 0.1f;
    public float fadeDuration = 0.5f;

    [Header("Idle Settings")]
    public float idleTimeThreshold = 30f;
    public string idleMessage = "If you're finished formatting, you can press Print in the toolbar!";

    private bool _isIdle = false;

    private VisualElement _root;
    private Label _bubbleText;
    private VisualElement _graphic;
    private Coroutine _animCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _idleCoroutine;

    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        _root = root.Q<VisualElement>("PennyRoot");
        _bubbleText = root.Q<Label>("PennyBubbleText");
        _graphic = root.Q<VisualElement>("PennyGraphic");

        // Force hidden on start
        if (_root != null) 
        {
            _root.style.opacity = 0f;
            _root.pickingMode = PickingMode.Ignore;
        }

        var closeBtn = _root?.Q<Button>("PennyClose");
        if (closeBtn != null) closeBtn.clicked += HideFeedback;

        // Listen for ANY interaction to reset the idle timer
        if (root != null)
        {
            root.RegisterCallback<PointerDownEvent>(ResetIdleTimer, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(ResetIdleTimer, TrickleDown.TrickleDown);
        }

        // Fetch dynamic idle message from GradingManager JSON if available
        var gm = Object.FindFirstObjectByType<GradingManager>();
        if (gm != null && gm.PennyScript != null && !string.IsNullOrEmpty(gm.PennyScript.idleMessage))
        {
            idleMessage = gm.PennyScript.idleMessage;
        }

        StartIdleTimer();
    }

    private void StartIdleTimer()
    {
        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        _isIdle = false;
        _idleCoroutine = StartCoroutine(IdleTimerRoutine());
    }

    private IEnumerator IdleTimerRoutine()
    {
        yield return new WaitForSeconds(idleTimeThreshold);
        _isIdle = true;
        ShowFeedback(idleMessage, false);
    }

    public void ShowFeedback(string message, bool centerOnScreen = false)
    {
        if (_root == null) return;
        
        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        
        if (centerOnScreen) _root.AddToClassList("penny-center");
        else _root.RemoveFromClassList("penny-center");
        
        _bubbleText.text = message;
        
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(1f));

        if (_animCoroutine == null && frames != null && frames.Count > 0)
            _animCoroutine = StartCoroutine(AnimateSprite());
    }

    private void HideFeedback()
    {
        if (_root == null) return;
        
        onFeedbackDismissed?.Invoke();
        onFeedbackDismissed = null;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(0f));

        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        StartIdleTimer();
    }

    private IEnumerator FadeRoutine(float targetOpacity)
    {
        // Enable clicks if fading in, disable if fading out
        _root.pickingMode = targetOpacity > 0.5f ? PickingMode.Position : PickingMode.Ignore;

        float startOpacity = _root.style.opacity.value;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _root.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / fadeDuration);
            yield return null;
        }
        
        _root.style.opacity = targetOpacity;
    }

    private IEnumerator AnimateSprite()
    {
        int currentFrame = 0;
        while (true)
        {
            if (_graphic != null)
                _graphic.style.backgroundImage = new StyleBackground(frames[currentFrame]);
            
            currentFrame = (currentFrame + 1) % frames.Count;
            yield return new WaitForSeconds(frameRate);
        }
    }

    private void ResetIdleTimer(EventBase evt)
    {
        if (_isIdle)
        {
            _isIdle = false;
            // Auto-hide her if she's currently showing the idle message and the user interacts
            if (_root != null && _bubbleText != null && _bubbleText.text == idleMessage)
            {
                HideFeedback();
            }
        }
        else
        {
            StartIdleTimer();
        }
    }
}
