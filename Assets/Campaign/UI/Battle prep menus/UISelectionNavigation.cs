using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BattlePrep
{
    public class UISelectionNavigation : Button
    {
        static UISelectionNavigation _previousSelectedElement;
        Selectable _defaultOnDown;
        protected override void Start()
        {
            PreparationContoller.OpenedTab += UpdateNavigation;
            _defaultOnDown = navigation.selectOnDown;
        }
        protected override void OnDestroy()
        {
            PreparationContoller.OpenedTab -= UpdateNavigation;
        }
        protected void UpdateNavigation(ITab tab)=>StartCoroutine(UpdateAfterWait(tab));
        IEnumerator UpdateAfterWait(ITab tab)
        {
            yield return null;
            Selectable selectable = tab.GetComponentInChildren<Selectable>();
            //set navigation
            Navigation NewNav = new Navigation();
            NewNav.mode = Navigation.Mode.Explicit;
            NewNav.selectOnRight = navigation.selectOnRight; NewNav.selectOnLeft = navigation.selectOnLeft;
            if (selectable == null) NewNav.selectOnDown = _defaultOnDown;
            else NewNav.selectOnDown = selectable;
            this.navigation = NewNav;
        }
        public override void OnSelect(BaseEventData eventData)
        {
            if (_previousSelectedElement != null && _previousSelectedElement != this)
                StartCoroutine(Reselect());
            base.OnSelect(eventData);
        }
        IEnumerator Reselect()
        {
            yield return null;
            _previousSelectedElement.Select();
        }
        public override void OnDeselect(BaseEventData eventData)
        {
            StartCoroutine(GetSelectedObject());
            base.OnDeselect(eventData);
        }
        IEnumerator GetSelectedObject()
        {
            _previousSelectedElement = null;
            yield return null;
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selected == null || selected.GetComponent<UISelectionNavigation>() == null)
                _previousSelectedElement = this;
        }
    }
}
