using System.Collections;
using NUnit.Framework;
using Supplement.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Supplement.Tests.PlayMode
{
    public class TestBackKeyReceiver
    {
        private GameObject root;
        private Keyboard keyboard;
        private InputSettings originalSettings;
        private InputSettings testSettings;

        [SetUp]
        public void SetUp()
        {
            // Unityやそのウィンドウにフォーカスが無くてもキーボードの入力が捨てられないようにする。プロジェクトの設定アセットは触らない。
            originalSettings = InputSystem.settings;
            testSettings = ScriptableObject.CreateInstance<InputSettings>();
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();

            root = new GameObject(nameof(TestBackKeyReceiver));
            new GameObject("EventSystem", typeof(EventSystem)).transform.SetParent(root.transform);
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.SetParent(root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            InputSystem.RemoveDevice(keyboard);
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
            }
            Object.Destroy(testSettings);
        }

        [UnityTest]
        public IEnumerator BackKeyClicksTopmostButtonOnce()
        {
            var clickCount = 0;
            var button = CreateButton("Button");
            button.onClick.AddListener(() => clickCount++);
            button.gameObject.AddComponent<BackKeyReceiver>();
            yield return null;

            yield return PressBackKey();

            Assert.AreEqual(1, clickCount);
        }

        [UnityTest]
        public IEnumerator CoveredButtonIsNotClicked()
        {
            var clickCount = 0;
            var button = CreateButton("Covered");
            button.onClick.AddListener(() => clickCount++);
            button.gameObject.AddComponent<BackKeyReceiver>();
            CreateButton("Cover");
            yield return null;

            yield return PressBackKey();

            Assert.AreEqual(0, clickCount);
        }

        [UnityTest]
        public IEnumerator ClosingTopDoesNotClickButtonBelowInSameFrame()
        {
            var bottomClickCount = 0;
            var bottom = CreateButton("Bottom");
            bottom.onClick.AddListener(() => bottomClickCount++);
            bottom.gameObject.AddComponent<BackKeyReceiver>();

            var top = CreateButton("Top");
            top.onClick.AddListener(() => Object.Destroy(top.gameObject));
            top.gameObject.AddComponent<BackKeyReceiver>();
            yield return null;

            yield return PressBackKey();

            Assert.IsTrue(top == null, "上のボタンが発火していない");
            Assert.AreEqual(0, bottomClickCount);
        }

        // 画面全体を覆うImage付きのButtonを、後から作ったものほど手前になるようCanvasの末尾に置く。
        private Button CreateButton(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(root.GetComponentInChildren<Canvas>().transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return gameObject.GetComponent<Button>();
        }

        private IEnumerator PressBackKey()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            // 押下が届いていないと、「発火しない」ことを確かめるテストが何も確かめずに通ってしまうため。
            Assert.IsTrue(keyboard.escapeKey.isPressed, "Escの押下がInput Systemに届いていない");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
    }
}