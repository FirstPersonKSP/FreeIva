using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FreeIva
{
	internal static class GuiTutorial
	{
		private static Rect windowPos = new Rect(Screen.width * 0.75f, Screen.height * 0.5f, 10, 10);
		public static bool Active = false;

		public static void Gui(int instanceId)
		{
			if (Active && Settings.ShowTutorial && !KerbalIvaAddon.Instance.buckled)
			{
				windowPos = GUILayout.Window(instanceId, windowPos, WindowGui, "FreeIva", GUILayout.Width(250), GUILayout.Height(200));
			}
		}

		static void WindowGui(int windowID)
		{
			GUILayout.BeginVertical();

			GuiUtils.label("Mouse Look", Mouse.Buttons.Right + " Click");
			GuiUtils.label("Forward", Settings.ForwardKey);
			GuiUtils.label("Backward", Settings.BackwardKey);
			GuiUtils.label("Strafe Left", Settings.LeftKey);
			GuiUtils.label("Strafe Right", Settings.RightKey);

			if (KerbalIvaAddon.KerbalIva.IsOnLadder || (!KerbalIvaAddon.KerbalIva.UseRelativeMovement() || KerbalIvaAddon.KerbalIva.KerbalCollisionTracker.RailColliderCount > 0))
			{
				GuiUtils.label("Move Up", Settings.UpKey);
				GuiUtils.label("Move Down", Settings.DownKey);
			}

			if (KerbalIvaAddon.KerbalIva.UseRelativeMovement())
			{
				GuiUtils.label("Crouch", Settings.CrouchKey);

				if (KerbalIvaAddon.KerbalIva.IsOnLadder)
				{
					GuiUtils.label("Release Ladder", Settings.JumpKey);
				}
				else
				{
					GuiUtils.label("Jump", Settings.JumpKey);
				}
			}
			else
			{
				GuiUtils.label("Roll CCW", Settings.RollCCWKey);
				GuiUtils.label("Roll CW", Settings.RollCWKey);
			}

			GuiUtils.label("Return to Seat", GameSettings.CAMERA_MODE.primary.code);

			if (KerbalIvaAddon.KerbalIva.UseRelativeMovement() || !KerbalIvaAddon.Gravity)
			{
				GuiUtils.label("Toggle Gravity", GameSettings.MODIFIER_KEY.primary.code + " + " + Settings.ToggleGravityKey);
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Close"))
			{
				Active = false;
			}
			if (GUILayout.Button("Close Forever"))
			{
				Active = false;
				Settings.ShowTutorial = false;
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUI.DragWindow();
		}
	}
}
