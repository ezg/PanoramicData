/* HatchingEffect by Charles Bissonnette (chabiss@digitalepiphania.com)

************************************TERMS AND CONDITIONS************************************************
Redistribution and use in source and binary forms, with or without modification, are permitted provided 
that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, 
   this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice, 
   this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE 
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

/********************************************************************
	created:	2009/07/02
	created:	2:7:2009   10:00
	filename: 	HatchingEffect\HatchingEffect.cs
	file path:	HatchingEffect
	file base:	HatchingEffect
	author:		Charles Bissonnette (chabiss@digitalepiphania.com)
	
	purpose:
 
	The hatching effect is a multi texture sampler shader that combines
	three textures based on the luminance of the input pixel sampler. Each texture
	match a luminance intensity and then blended together. 
	
	The shader has three textures:LightToneTexture, MiddleToneTexture and
	DarkToneTexture and for Threshold setting that control the dominance
	of each texture. The default textures used for the three input can
	be overridden in XAML by specifying a different image brush to the inputs. 
*********************************************************************/
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using ShaderEffectLibrary;

namespace ShaderEffectLibrary
{
	public class HatchingEffect : ShaderEffect
	{
		#region Constructors

		static HatchingEffect()
		{
			pixelShader.UriSource = Global.MakePackUri("ShaderSource/Hatching.ps");
		}

		public HatchingEffect()
		{
			this.PixelShader = pixelShader;

			this.LightToneTexture = this.LoadImageBrush("Textures/LightToneTexture.png");
			this.MiddleToneTexture = this.LoadImageBrush("Textures/MiddleToneTexture.png");
			this.DarkToneTexture = this.LoadImageBrush("Textures/DarkToneTexture.png");

			// Update each DependencyProperty that's registered with a shader register.  This
			// is needed to ensure the shader gets sent the proper default value.
			UpdateShaderValue(InputProperty);
			UpdateShaderValue(LightToneTextureProperty);
			UpdateShaderValue(MiddleToneTextureProperty);
			UpdateShaderValue(DarkToneTextureProperty);

			UpdateShaderValue(TransparentToneThresholdProperty);
			UpdateShaderValue(LightToneThresholdProperty);
			UpdateShaderValue(MiddleToneThresholdProperty);
			UpdateShaderValue(DarkToneThresholdProperty);	
		}

		#endregion

		#region Dependency Properties

		public Brush Input
		{
			get { return (Brush)GetValue(InputProperty); }
			set { SetValue(InputProperty, value); }
		}

		// Brush-valued properties turn into sampler-property in the shader.
		// This helper sets "ImplicitInput" as the default, meaning the default
		// sampler is whatever the rendering of the element it's being applied to is.
		public static readonly DependencyProperty InputProperty =
			ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(HatchingEffect), 0);

		public Brush LightToneTexture
		{
			get { return (Brush)GetValue(LightToneTextureProperty); }
			set { SetValue(LightToneTextureProperty, value); }
		}

		// Brush-valued properties turn into sampler-property in the shader.
		// This helper sets "ImplicitInput" as the default, meaning the default
		// sampler is whatever the rendering of the element it's being applied to is.
		public static readonly DependencyProperty LightToneTextureProperty =
			ShaderEffect.RegisterPixelShaderSamplerProperty("LightToneTexture", typeof(HatchingEffect), 1);

		public Brush MiddleToneTexture
		{
			get { return (Brush)GetValue(MiddleToneTextureProperty); }
			set { SetValue(MiddleToneTextureProperty, value); }
		}

		// Brush-valued properties turn into sampler-property in the shader.
		// This helper sets "ImplicitInput" as the default, meaning the default
		// sampler is whatever the rendering of the element it's being applied to is.
		public static readonly DependencyProperty MiddleToneTextureProperty =
			ShaderEffect.RegisterPixelShaderSamplerProperty("MiddleToneTexture", typeof(HatchingEffect), 2);

		public Brush DarkToneTexture
		{
			get { return (Brush)GetValue(DarkToneTextureProperty); }
			set { SetValue(DarkToneTextureProperty, value); }
		}

		// Brush-valued properties turn into sampler-property in the shader.
		// This helper sets "ImplicitInput" as the default, meaning the default
		// sampler is whatever the rendering of the element it's being applied to is.
		public static readonly DependencyProperty DarkToneTextureProperty =
			ShaderEffect.RegisterPixelShaderSamplerProperty("DarkToneTexture", typeof(HatchingEffect), 3);

		//////////////////////////////////////////////////////////////////////////
		// Thresholds 
		//////////////////////////////////////////////////////////////////////////

		public double TransparentToneThreshold
		{
			get { return (double)GetValue(TransparentToneThresholdProperty); }
			set { SetValue(TransparentToneThresholdProperty, value); }
		}

		// Scalar-valued properties turn into shader constants with the register
		// number sent into PixelShaderConstantCallback().
		public static readonly DependencyProperty TransparentToneThresholdProperty =
			DependencyProperty.Register("TransparentToneThreshold", typeof(double), typeof(HatchingEffect),
					new PropertyMetadata(4.0, PixelShaderConstantCallback(0)));

		public double LightToneThreshold
		{
			get { return (double)GetValue(LightToneThresholdProperty); }
			set { SetValue(LightToneThresholdProperty, value); }
		}

		// Scalar-valued properties turn into shader constants with the register
		// number sent into PixelShaderConstantCallback().
		public static readonly DependencyProperty LightToneThresholdProperty =
			DependencyProperty.Register("LightToneThreshold", typeof(double), typeof(HatchingEffect),
					new PropertyMetadata(3.0, PixelShaderConstantCallback(1)));

		public double MiddleToneThreshold
		{
			get { return (double)GetValue(MiddleToneThresholdProperty); }
			set { SetValue(MiddleToneThresholdProperty, value); }
		}

		// Scalar-valued properties turn into shader constants with the register
		// number sent into PixelShaderConstantCallback().
		public static readonly DependencyProperty MiddleToneThresholdProperty =
			DependencyProperty.Register("MiddleToneThreshold", typeof(double), typeof(HatchingEffect),
					new PropertyMetadata(2.0, PixelShaderConstantCallback(2)));

		public double DarkToneThreshold
		{
			get { return (double)GetValue(DarkToneThresholdProperty); }
			set { SetValue(DarkToneThresholdProperty, value); }
		}

		// Scalar-valued properties turn into shader constants with the register
		// number sent into PixelShaderConstantCallback().
		public static readonly DependencyProperty DarkToneThresholdProperty =
			DependencyProperty.Register("DarkToneThreshold", typeof(double), typeof(HatchingEffect),
					new PropertyMetadata(1.0, PixelShaderConstantCallback(3)));

		#endregion

		#region Member Data

		private static PixelShader pixelShader = new PixelShader();

		ImageBrush LoadImageBrush(string imageSource)
		{
			ImageBrush brush = new ImageBrush();
			brush.ImageSource = new BitmapImage(Global.MakePackUri(imageSource));
			return brush;
		}

		#endregion

	}
}
