using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace Supplement.Unity
{
    /// <summary>
    /// <see cref="Button"/> を持つGameObjectにアタッチし、Androidの戻るボタン(Editor/キーボードではEscキー)で
    /// そのButtonのクリックを発火させる。
    /// </summary>
    /// <remarks>
    /// ナビゲーションスタックの知識は持たず、自身のRectTransformが画面上でRaycast的にトップにあるかどうかだけで
    /// 発火の可否を判定する。トップ判定は中心+4隅の5点で行い、いずれか1点でもヒットすればトップとみなす。
    /// 入力の監視はインスタンスごとのUpdateではなく<see cref="BackKeyDispatcher"/>が1か所で行う。
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
        private bool isConstructed;
        private bool isRegistered;

        [Inject]
        internal void Construct(IObjectResolver objectResolver)
        {
            objectResolver.TryResolve(out debugRegistry);
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
            BackKeyDispatcher.Add(this);
            // Constructの方が先に済んでいれば、ここで登録する。まだならConstruct側に任せる。
            if (isConstructed)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            BackKeyDispatcher.Remove(this);
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

        /// <summary>
        /// 戻るキーが押されたフレームに、Buttonのクリックを発火させてよいか。
        /// </summary>
        internal bool CanInvoke()
        {
            return button.interactable && IsTargetOnTop();
        }

        /// <summary>
        /// Buttonのクリックを発火させる。<see cref="CanInvoke"/>の判定で使ったPointerEventDataを渡す。
        /// </summary>
        internal void Invoke()
        {
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
        private Vector2[] GetSamplePoints()
        {
            WriteLocalSamplePoints(GetInnerRect(rectTransform.rect), samplePoints);

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

        private Rect GetInnerRect(Rect rect)
        {
            // insetPixelsがRectの半分より大きいと内側に潰れて反転してしまうため、上限をかける。
            var inset = Mathf.Min(insetPixels, rect.width / 2f, rect.height / 2f);
            return Rect.MinMaxRect(
                rect.xMin + inset, rect.yMin + inset,
                rect.xMax - inset, rect.yMax - inset);
        }

        private static void WriteLocalSamplePoints(Rect innerRect, Vector2[] points)
        {
            points[0] = innerRect.center;
            points[1] = new Vector2(innerRect.xMin, innerRect.yMax);
            points[2] = new Vector2(innerRect.xMax, innerRect.yMax);
            points[3] = new Vector2(innerRect.xMin, innerRect.yMin);
            points[4] = new Vector2(innerRect.xMax, innerRect.yMin);
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
                throw new InvalidOperationException(
                    $"{nameof(BackKeyReceiver)} on '{name}' must be under a Canvas.");
            }
            return canvas;
        }

        private bool IsPointOnTop(Vector2 screenPoint)
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

#if UNITY_EDITOR
        private const float GizmoPointSize = 8f;
        // 実行中の判定(samplePoints)と混ざらないよう、Scene Viewの表示には別のバッファを使う。
        private static readonly Vector2[] GizmoPoints = new Vector2[5];

        /// <summary>
        /// 選択中のReceiverについて、トップ判定点を囲む枠と判定点(中心+4隅)をScene Viewに水色で表示する。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Awake前(編集時)にも呼ばれるため、キャッシュしたrectTransformは使わない。
            var rt = (RectTransform)transform;
            var innerRect = GetInnerRect(rt.rect);
            WriteLocalSamplePoints(innerRect, GizmoPoints);

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = rt.localToWorldMatrix;

            Gizmos.DrawWireCube(innerRect.center, innerRect.size);
            foreach (var point in GizmoPoints)
            {
                Gizmos.DrawCube(point, new Vector3(GizmoPointSize, GizmoPointSize, 0f));
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
#endif
    }
}
