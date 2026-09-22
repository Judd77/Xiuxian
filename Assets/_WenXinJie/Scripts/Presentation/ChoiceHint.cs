using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    public sealed class ChoiceHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Text _target;
        private string _message;

        public void Configure(Text target, string message)
        {
            _target = target;
            _message = message;
        }

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
    }
}
