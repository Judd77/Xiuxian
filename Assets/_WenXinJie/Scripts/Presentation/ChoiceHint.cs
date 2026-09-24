using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WenXinJie.Presentation
{
    public sealed class ChoiceHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _message;
        private TMP_Text _target;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _target.text = _message;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_target.text == _message)
            {
                _target.text = string.Empty;
            }
        }

        public void Configure(TMP_Text target, string message)
        {
            _target = target;
            _message = message;
        }
    }
}