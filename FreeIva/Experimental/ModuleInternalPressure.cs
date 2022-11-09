#if Experimental
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
	public class ModuleInternalPressure : PartModule
	{
		public bool isPressurisable { get; set; }
		public double internalVolume { get; set; }
		public double pressure { get; set; }
		public double temperature { get { return part.temperature; } }

		public const double VacuumPressure = 0.00000000001322; // Pa
		public const double OneAtm = 101.325; // kPa
		public const double HatchDiameter = 1.2; // m
		public const double HatchRadius = 0.6; // m
		public const double HatchHandleDistance = 0.79; // m from hinge
		public static double StandardHatchArea = 1.131; // m² for ⌀ 1.2m hatch.
														// TODO: Read this from kerbal skills.
		public static float KerbalLiftingForce = 600; // N
		public static float KerbalLiftingForceWhenUsingHandle = 790; // N, 1.316- x advantage

		public double targetPressure = OneAtm;

		// Flow rate => Air temp, pressure before, pressure after, diameter
		// Pressure = Pascal
		// 1 Pa = 1 N/m² = 1 kg/(m·s²)
		// Hatch diameter = ~1.2m
		// Standard air pressure = 101.325 kPa abs
		// Venting to vacuum = "Choked flow"
		// http://www.geoffreylandis.com/higgins.html
		// Time to fully depressurise for a craft containing air, at room temperature of 293K, assuming isothermal blow-down:
		// t = 0.086 (V/A) Ln[pi/pf]/(Sqrt[T])
		// where V = volume (m³), A = area of the hole (m²), pi = initial pressure, pf = final pressure, T = spacecraft temperature (K)

		public void Start()
		{

		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasValue("isPressurisable"))
				isPressurisable = bool.Parse(node.GetValue("isPressurisable"));

			if (node.HasValue("internalVolume"))
				internalVolume = double.Parse(node.GetValue("internalVolume"));

			if (isPressurisable && node.HasValue("pressure"))
				pressure = double.Parse(node.GetValue("pressure"));
		}

		public override void OnSave(ConfigNode node)
		{
			if (isPressurisable)
				node.SetValue("pressure", pressure, true);
		}

		public static double TimeToDrop(Part part, float volume, float area, float initialPressure, float finalPressure)
		{
			double temperature = part.temperature;
			return 0.086 * (volume / area) * Math.Log(initialPressure / finalPressure) / Math.Sqrt(temperature);
		}

		public bool CanOpenHatch(float kerbalLiftingForceN, double internalPressurePa, double externalPressurePa, double hatchArea)
		{
			// TODO: If external pressure > internal pressure, hatch should be propelled inwards.
			// TODO: Add the mechanical advantage of using the handle.
			return Convert.ToDouble(kerbalLiftingForceN) > ((internalPressurePa - externalPressurePa) / hatchArea);
		}

		public void Pressurise()
		{
			// TODO: Draw resources.
			ChangePressure(OneAtm);
		}

		public void Depressurise()
		{
			ChangePressure(vessel.atmDensity);
		}

		public void ChangePressure(double newPressure)
		{
			targetPressure = newPressure;
		}

		private double changeRate = 1; // TODO: Replace
		public void Update()
		{
			if (pressure != targetPressure)
			{
				pressure += (targetPressure - pressure) * Time.deltaTime * changeRate;
			}
		}
	}
}
#endif
