using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Effects;
using ShaderEffectLibrary;
using System.Windows;

namespace Cjc.ThreeDeemium.Effects
{
	public class EffectsHelper
	{
		public static IDictionary<string, ShaderEffect> GetEffects()
		{
			return new Dictionary<string, ShaderEffect>
			{
				{ "No effect", null },
				{ "Hatch", new HatchingEffect() },
				{ "Ripple", new RippleEffect { Amplitude = 0.1, Frequency = 10 } },
				{ "Toon", new ToonShaderEffect() },
				{ "Magnify", new MagnifyEffect { Center = new Point( 0.5, 0.5 ), Radii = new Size( 0.5, 0.5 ) } },
				{ "Pinch", new PinchEffect { Amount = 3, Radius = 0.5 } },
				{ "Swirl", new SwirlEffect { SwirlStrength = 0.2 } },
				{ "Pixelate", new PixelateEffect { HorizontalPixelCounts = 200, VerticalPixelCounts = 200 } },
				{ "Zoom", new ZoomBlurEffect { BlurAmount = 0.05 } }
			};
		}
	}
}