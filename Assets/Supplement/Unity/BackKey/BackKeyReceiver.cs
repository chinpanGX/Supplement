using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Supplement.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

[assembly: InternalsVisibleTo("Supplement.Unity.Editor")]
namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="Button"/> を持つGameObjectにアタッチし、Androidの戻るボタン(Editor/キーボードではEscキー)で
    /// そのButtonのクリックを発火させる。
    /// </summary>
    /// <remarks>
    /// ナビゲーションスタックの知識は持たず、自身のRectTransformが画面上でRaycast的にトップにあるかどうかだけで
    /// 発火の可否を判定する。トップ判定は中心+4隅の5点で行い、いずれか1点でもヒットすればトップとみなす。
    /// </remarks>
    [RequireComponent(typeof(Button), typeof(RectTransform))]
    public class BackKeyReceiver : MonoBehaviour
    {
        [Header("トップ判定点(中心+4隅)を、Rectの境界から内側にずらすピクセル数")]
        [SerializeField] private float insetPixels = 4f;

        private readonly Vector2[] samplePoints = new Vector2[5];
        private readonly List<RaycastResult> raycastResultsBuffer = new();

        private Button button;
        private RectTransform rectTransform;
        private Canvas canvas;
        private PointerEventData pointerEventData;
        private BackKeyReceiverDebugRegistry debugRegistry;
        private IDebugOverlayState debugOverlayState;
        private bool isConstructed;
        private bool isRegistered;

        [Inject]
        internal void Construct(IObjectResolver objectResolver)
        {
            objectResolver.TryResolve(out debugRegistry);
            objectResolver.TryResolve(out debugOverlayState);
            isConstructed = true;
            // OnEnableの方が先に来ていた場合(USN側のInstantiateと同時に同期的に発火するため、
            // Injectより先にOnEnableが走ることがある)、ここで登録する。
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = (RectTransform)transform;
        }

        private void OnEnable()
        {
            // Constructの方が先に済んでいれば、ここで登録する。まだならConstruct側に任せる。
            if (isConstructed)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            if (isRegistered)
            {
                return;
            }
            isRegistered = true;
            debugRegistry?.Register(this);
        }

        private void Unregister()
        {
            if (!isRegistered)
            {
                return;
            }
            isRegistered = false;
            debugRegistry?.Unregister(this);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (!button.interactable)
            {
                return;
            }

            if (!IsTargetOnTop())
            {
                return;
            }
            
            ExecuteEvents.Execute(
                button.gameObject,
                pointerEventData,
                ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// 自身のRectTransformが、現在画面上でRaycast的にトップにあるかどうかを判定する。
        /// </summary>
        public bool IsTargetOnTop()
        {
            foreach (var point in GetSamplePoints())
            {
                if (IsPointOnTop(point))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判定に使う5点(中心+4隅)を、インスタンス保持の配列に書き込んで返す。
        /// 呼び出しのたびに内容が上書きされるため、結果を後で使う場合は呼び出し側でコピーすること。
        /// </summary>
        internal Vector2[] GetSamplePoints()
        {
            var rect = rectTransform.rect;
            // insetPixelsがRectの半分より大きいと内側に潰れて反転してしまうため、上限をかける。
            var inset = Mathf.Min(insetPixels, rect.width / 2f, rect.height / 2f);
            var innerRect = Rect.MinMaxRect(
                rect.xMin + inset, rect.yMin + inset,
                rect.xMax - inset, rect.yMax - inset);

            samplePoints[0] = innerRect.center;
            samplePoints[1] = new Vector2(innerRect.xMin, innerRect.yMax);
            samplePoints[2] = new Vector2(innerRect.xMax, innerRect.yMax);
            samplePoints[3] = new Vector2(innerRect.xMin, innerRect.yMin);
            samplePoints[4] = new Vector2(innerRect.xMax, innerRect.yMin);

            var camera = GetCanvas().worldCamera;
            for (var i = 0; i < samplePoints.Length; i++)
            {
                var worldPoint = rectTransform.TransformPoint(samplePoints[i]);
                var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
                screenPoint.x = Mathf.Clamp(screenPoint.x, 0, Screen.width);
                screenPoint.y = Mathf.Clamp(screenPoint.y, 0, Screen.height);
                samplePoints[i] = screenPoint;
            }
            return samplePoints;
        }

        /// <summary>
        /// 親Canvasを取得する。AwakeやConstructの時点ではCanvas階層への親付けが済んでいない
        /// ことがあるため、初回利用時に遅延取得してキャッシュする(親子関係は以降変わらない前提)。
        /// </summary>
        /// <remarks>
        /// このキャッシュは「一度アタッチされたら別のCanvas配下へ再親付け/使い回しされない」ことが前提。
        /// UnityScreenNavigator経由の利用(Push毎にInstantiate、Popで破棄)ではこの前提が成り立つが、
        /// オブジェクトプーリングなどでGameObjectを別Canvas配下に移して再利用する構成で使う場合は注意すること。
        /// </remarks>
        private Canvas GetCanvas()
        {
            if (canvas != null)
            {
                return canvas;
            }

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(BackKeyReceiver)} on '{name}' must be under a Canvas.");
            }
            return canvas;
        }

        internal bool IsPointOnTop(Vector2 screenPoint)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            pointerEventData ??= new PointerEventData(EventSystem.current);
            pointerEventData.position = screenPoint;

            raycastResultsBuffer.Clear();
            EventSystem.current.RaycastAll(pointerEventData, raycastResultsBuffer);
            if (raycastResultsBuffer.Count == 0)
            {
                return false;
            }

            var hitTransform = raycastResultsBuffer[0].gameObject.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        private void OnGUI()
        {
            if (debugOverlayState is not { Enabled: true })
            {
                return;
            }

            BackKeyDebugOverlayDrawer.Draw(this);
        }
    }
}
