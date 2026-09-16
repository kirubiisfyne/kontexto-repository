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
    private bool _idleSuppressed = false;

    private VisualElement _root;
    private Label _bubbleText;
    private VisualElement _graphic;
    private Coroutine _animCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _idleCoroutine;

    /// <summary>Penny's root element, so other systems can measure and position her.</summary>
    public VisualElement Root => _root;

    /// <summary>True while her bubble is at least partly visible.</summary>
    public bool IsShowing => _root != null && _root.resolvedStyle.opacity > 0.01f;

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
        if (_idleSuppressed) return;

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

    public void HideFeedback()
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

    /// <summary>
    /// Sets the bubble text without fading her in, so a caller can wait for the bubble to resize,
    /// position her, and only then call <see cref="ShowFeedback"/>.
    /// </summary>
    public void SetMessage(string message)
    {
        if (_bubbleText == null) return;
        _bubbleText.text = message;
    }

    /// <summary>
    /// Pins Penny to an absolute position in panel space, overriding her docked USS anchors.
    /// Call <see cref="ClearAnchor"/> to hand her back to USS.
    /// </summary>
    public void SetAnchor(Vector2 position)
    {
        if (_root == null) return;

        // .penny-center also applies a translate, which would offset the anchored position.
        _root.RemoveFromClassList("penny-center");

        _root.style.left = position.x;
        _root.style.top = position.y;

        // Auto, not Null: left+right (or top+bottom) both being set would stretch her to fit.
        _root.style.right = StyleKeyword.Auto;
        _root.style.bottom = StyleKeyword.Auto;
    }

    /// <summary>Hands Penny back to her docked USS position.</summary>
    public void ClearAnchor()
    {
        if (_root == null) return;

        _root.style.left = StyleKeyword.Null;
        _root.style.top = StyleKeyword.Null;
        _root.style.right = StyleKeyword.Null;
        _root.style.bottom = StyleKeyword.Null;
    }

    /// <summary>
    /// Stops Penny from showing her idle message. A scripted sequence turns this on while it owns
    /// the bubble so the idle nag cannot overwrite a scripted line; turning it off restarts the countdown.
    /// </summary>
    public void SetIdleSuppressed(bool suppressed)
    {
        _idleSuppressed = suppressed;
        _isIdle = false;

        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
            _idleCoroutine = null;
        }

        // Starting a coroutine on a component that is being disabled throws, and the tutorial calls
        // this from its OnDisable. Penny's own OnEnable restarts the countdown once we are live again.
        if (!suppressed && isActiveAndEnabled) StartIdleTimer();
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
        // A scripted sequence owns the bubble while the idle nag is suppressed, and the player's
        // clicks during that sequence must not restart the countdown under it.
        if (_idleSuppressed) return;

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
