using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RobotRun
{
    public sealed partial class RoomView
    {
        private bool menuPreview;
        private SpriteRenderer menuLeftDoor;

        internal void BuildMenuPreview()
        {
            var terrain = new HashSet<Vector2Int>();
            for (int x = 0; x < 13; x++)
                terrain.Add(new Vector2Int(x, 0));
            var definition = new LevelDefinition(new Vector2Int(13, 4), new Vector2Int(6, 1),
                new Vector2Int(10, 1), terrain, false, default, default, default);
            var existingCamera = Camera.main;
            Build(definition);
            menuPreview = true;
            roomCamera.rect = new Rect(0.125f, 0.26f, 0.75f, 0.38f);
            roomCamera.depth = existingCamera != null ? existingCamera.depth + 1f : 1f;
            if (existingCamera != null && existingCamera.TryGetComponent<AudioListener>(out var listener))
                listener.enabled = false;

            transform.Find("Exit label").GetComponent<TextMeshPro>().text = "INIZIA";
            var position = new Vector2(2, 1);
            Block(transform, "Exit menu frame", position + Vector2.down * 0.035f,
                new Vector2(0.73f, 0.93f), MockupTheme.Coral, 4);
            menuLeftDoor = Block(transform, "Exit menu inset", position + Vector2.down * 0.033f,
                new Vector2(0.67f, 0.86f), new Color(0.18f, 0.10f, 0.08f), 5);
            var label = WorldLabel(transform, "Exit menu label", position + Vector2.up * 0.56f,
                new Vector2(1, 0.18f), 1.3f);
            label.text = "ESCI";
            label.color = MockupTheme.Coral;
            var arrow = WorldLabel(transform, "Exit menu arrow", position, Vector2.one * 0.5f, 3f);
            arrow.text = "←";
            arrow.color = MockupTheme.Coral;
            RenderWorld(new GridSimulation(definition, Array.Empty<CardInstruction?>()).State);
            FrameRoom();
        }

        internal void SetMenuDirection(int direction)
        {
            menuLeftDoor.color = direction < 0 ? new Color(0.43f, 0.22f, 0.12f) : new Color(0.18f, 0.10f, 0.08f);
            exitFocus.SetActive(direction > 0);
        }

        internal void CompleteMenuRun(Vector2Int destination)
        {
            robot.gameObject.SetActive(false);
            robotFocus.SetActive(false);
            exitFocus.transform.localPosition = (Vector2)destination;
            exitFocus.SetActive(true);
            foreach (var mark in trailMarks)
                mark.color = MockupTheme.Mint;
            PlaySound(successSound);
        }
    }
}