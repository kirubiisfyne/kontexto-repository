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

    private VisualElement _root;
    private Label _bubbleText;
    private VisualElement _graphic;
    private Coroutine _animCoroutine;
    private Coroutine _fadeCoroutine;

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
    }

    public void ShowFeedback(string message, bool centerOnScreen = false)
    {
        if (_root == null) return;
        
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
}
