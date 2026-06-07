using JRogue.World.Generation.Zones;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.World
{
    public sealed class DungeonZoneLayoutEditorWindow : EditorWindow
    {
        DungeonFloorZoneLayout _layout;
        Vector2 _scroll;

        [MenuItem("JRogue/World/Dungeon Zone Layout Preview")]
        public static void Open()
        {
            GetWindow<DungeonZoneLayoutEditorWindow>("Zone Layout");
        }

        void OnGUI()
        {
            _layout = (DungeonFloorZoneLayout)EditorGUILayout.ObjectField(
                "Layout Asset",
                _layout,
                typeof(DungeonFloorZoneLayout),
                false);

            if (_layout == null)
            {
                EditorGUILayout.HelpBox("Assign a DungeonFloorZoneLayout asset to preview pieces.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Floor Size", $"{_layout.FloorWidth} x {_layout.FloorHeight}");
            EditorGUILayout.LabelField("Layout Kind", _layout.LayoutKind.ToString());
            EditorGUILayout.LabelField("Fallback Zone", _layout.FallbackZoneId);
            EditorGUILayout.LabelField("Skeleton Stamp", _layout.SkeletonStamp != null ? _layout.SkeletonStamp.name : "(none)");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            ZoneLayoutPiece[] pieces = _layout.Pieces;
            if (pieces == null || pieces.Length == 0)
            {
                EditorGUILayout.HelpBox("No layout pieces authored.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    DrawPiece(pieces[i], i);
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Selection Rules", EditorStyles.boldLabel);
            ZoneSelectionRule[] rules = _layout.SelectionRules;
            if (rules == null || rules.Length == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                for (int i = 0; i < rules.Length; i++)
                    EditorGUILayout.LabelField($"• {rules[i].zoneId} weight={rules[i].weight} max={rules[i].maxInstances}");
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Preview Resolved Layout (seed 12345)"))
                PreviewLayout(12345);
        }

        void DrawPiece(ZoneLayoutPiece piece, int index)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Piece {index}: {piece.pieceId}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mandatory", piece.mandatory.ToString());
            EditorGUILayout.LabelField("Player Start", piece.isPlayerStartPiece.ToString());
            EditorGUILayout.LabelField("Anchor", piece.anchorKind.ToString());
            if (piece.anchorKind == ZonePieceAnchorKind.Compass)
                EditorGUILayout.LabelField("Compass", piece.compassDirection.ToString());
            else
                EditorGUILayout.LabelField(
                    "Normalized Rect",
                    $"({piece.normalizedRect.xMin:0.##},{piece.normalizedRect.yMin:0.##})-({piece.normalizedRect.xMax:0.##},{piece.normalizedRect.yMax:0.##})");

            EditorGUILayout.LabelField("Default Boundary", piece.defaultBoundary.ToString());
            if (piece.connectsTo != null && piece.connectsTo.Length > 0)
                EditorGUILayout.LabelField("Connects To", string.Join(", ", piece.connectsTo));

            if (piece.candidates != null)
            {
                for (int c = 0; c < piece.candidates.Length; c++)
                {
                    EditorGUILayout.LabelField(
                        $"  candidate {piece.candidates[c].zoneId} weight={piece.candidates[c].weight}");
                }
            }

            EditorGUILayout.EndVertical();
        }

        void PreviewLayout(int seed)
        {
            var rng = new System.Random(seed);
            ZoneSelectionResult result = ZoneSelectionSolver.Resolve(_layout, rng);
            if (!result.Success)
            {
                Debug.LogWarning($"[ZoneLayoutPreview] Selection failed: {result.FailureReason}");
                return;
            }

            var log = new System.Text.StringBuilder();
            log.AppendLine($"[ZoneLayoutPreview] seed={seed} kind={_layout.LayoutKind} pieces={result.Pieces.Length}");
            for (int i = 0; i < result.Pieces.Length; i++)
            {
                ResolvedZonePiece piece = result.Pieces[i];
                log.AppendLine($"  {piece.ZoneInstanceId} bounds={piece.Bounds}");
            }

            Debug.Log(log.ToString());
        }
    }
}
