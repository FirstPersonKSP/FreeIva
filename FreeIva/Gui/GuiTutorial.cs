using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace FreeIva
{
	internal static class GuiTutorial
	{
		// Localisation strings
		private static readonly string label_MouseLook = Localizer.Format("#FreeIVA_Tutorial_MouseLook");
		private static readonly string label_Forward = Localizer.Format("#FreeIVA_Tutorial_Forward");
		private static readonly string label_Backward = Localizer.Format("#FreeIVA_Tutorial_Backward");
		private static readonly string label_StrafeLeft = Localizer.Format("#FreeIVA_Tutorial_StrafeLeft");
		private static readonly string label_StrafeRight = Localizer.Format("#FreeIVA_Tutorial_StrafeRight");
		private static readonly string label_MoveUp = Localizer.Format("#FreeIVA_Tutorial_MoveUp");
		private static readonly string label_MoveDown = Localizer.Format("#FreeIVA_Tutorial_MoveDown");
		private static readonly string label_Crouch = Localizer.Format("#FreeIVA_Tutorial_Crouch");
		private static readonly string label_ReleaseLadder = Localizer.Format("#FreeIVA_Tutorial_ReleaseLadder");
		private static readonly string label_Jump = Localizer.Format("#FreeIVA_Tutorial_Jump");
		private static readonly string label_RollCCW = Localizer.Format("#FreeIVA_Tutorial_RollCCW");
		private static readonly string label_RollCW = Localizer.Format("#FreeIVA_Tutorial_RollCW");
		private static readonly string label_ReturnSeat = Localizer.Format("#FreeIVA_Tutorial_ReturnSeat");
		private static readonly string label_ToggleGravity = Localizer.Format("#FreeIVA_Tutorial_ToggleGravity");
		private static readonly string button_Close = Localizer.Format("#FreeIVA_Tutorial_Close");
		private static readonly string button_CloseForever = Localizer.Format("#FreeIVA_Tutorial_CloseForever");

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

			GuiUtils.label(label_MouseLook, Mouse.Buttons.Right + " Click"); // "Mouse Look"
			GuiUtils.label(label_Forward, Settings.ForwardKey); // "Forward"
			GuiUtils.label(label_Backward, Settings.BackwardKey); // "Backward"
			GuiUtils.label(label_StrafeLeft, Settings.LeftKey); // "Strafe Left"
			GuiUtils.label(label_StrafeRight, Settings.RightKey); // "Strafe Right"

			if (KerbalIvaAddon.Instance.KerbalIva.IsOnLadder || (!KerbalIvaAddon.Instance.KerbalIva.UseRelativeMovement() || KerbalIvaAddon.Instance.KerbalIva.KerbalCollisionTracker.RailColliderCount > 0))
			{
				GuiUtils.label(label_MoveUp, Settings.UpKey); // "Move Up"
				GuiUtils.label(label_MoveDown, Settings.DownKey); // "Move Down"
			}

			if (KerbalIvaAddon.Instance.KerbalIva.UseRelativeMovement())
			{
				GuiUtils.label(label_Crouch, Settings.CrouchKey); // "Crouch"

				if (KerbalIvaAddon.Instance.KerbalIva.IsOnLadder)
				{
					GuiUtils.label(label_ReleaseLadder, Settings.JumpKey); // "Release Ladder"
				}
				else
				{
					GuiUtils.label(label_Jump, Settings.JumpKey); // "Jump"
				}
			}
			else
			{
				GuiUtils.label(label_RollCCW, Settings.RollCCWKey); // "Roll CCW"
				GuiUtils.label(label_RollCW, Settings.RollCWKey); // "Roll CW"
			}

			GuiUtils.label(label_ReturnSeat, GameSettings.CAMERA_MODE.primary.code); // "Return to Seat"

			if (KerbalIvaAddon.Instance.KerbalIva.UseRelativeMovement() || !KerbalIvaAddon.Instance.Gravity)
			{
				GuiUtils.label(label_ToggleGravity, GameSettings.MODIFIER_KEY.primary.code + " + " + Settings.ToggleGravityKey); // "Toggle Gravity"
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(button_Close)) // "Close"
			{
				Active = false;
			}
			if (GUILayout.Button(button_CloseForever)) // "Close Forever"
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
