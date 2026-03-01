using UnityEngine;
using UnityEngine.UI;

namespace SkillTree.Demo
{
    public sealed class SkillTreeConnectionView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _lineImage;
        [SerializeField] private float _thickness = 6f;

        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;
        }

        public void SetEndpoints(Vector2 from, Vector2 to, float thickness = -1f)
        {
            if (_rectTransform == null)
                return;

            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f)
                length = 0.001f;

            float lineThickness = thickness > 0f ? thickness : _thickness;
            _rectTransform.sizeDelta = new Vector2(length, lineThickness);
            _rectTransform.anchoredPosition = (from + to) * 0.5f;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetColor(Color color)
        {
            if (_lineImage != null)
                _lineImage.color = color;
        }
    }
}
