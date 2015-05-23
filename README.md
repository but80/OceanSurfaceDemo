[日本語はこちら](http://qiita.com/but80/items/eed81a89ff23ee629bdd)

# Demonstration rippling Water4Advance (from the Unity Standard Assets) by a ComputeShader

![Screen shot](https://qiita-image-store.s3.amazonaws.com/0/34010/75a58e12-8e61-84be-2263-a893db8ccdff.png)

## How to run

1. Create a new project.
2. Select ``Menu > Assets > Import Package > Environment``.
3. Press ``None`` button on the Import dialog, then check ``Standard Assets/Environment/Water`` to include all assets under this directory. After that, press ``Import`` button.
4. Import ``OceanSurface.unitypackage`` in this project.
5. Open ``OceanSurface/OceanSurfaceSampleScene``.
6. Edit ``Assets/Standard Assets/Environment/Water/Water4/Shaders/FXWater4Advanced`` as follows (after the line 114, with attention to comments with ``INSERTED`` and ``REMOVED``) :


```c:FXWater4Advanced.shader
	
	// foam
	uniform float4 _Foam;
	
	// !!! INSERTED
	#include "../../../../../OceanSurface/OceanSurfaceInclude.cginc"
	// !!! /INSERTED
	
	// shortcuts
	#define PER_PIXEL_DISPLACE _DistortParams.x
	#define REALTIME_DISTORTION _DistortParams.y
	#define FRESNEL_POWER _DistortParams.z
	#define VERTEX_WORLD_NORMAL i.normalInterpolator.xyz
	#define FRESNEL_BIAS _DistortParams.w
	#define NORMAL_DISPLACEMENT_PER_VERTEX _InvFadeParemeter.z
	
	//
	// HQ VERSION
	//
	
	v2f vert(appdata_full v)
	{
		v2f o;
				
		half3 worldSpaceVertex = mul(_Object2World,(v.vertex)).xyz;
		half3 vtxForAni = (worldSpaceVertex).xzz;

		half3 nrml;
		half3 offsets;
		
		//!!! REMOVED
		//Gerstner (
		//	offsets, nrml, v.vertex.xyz, vtxForAni,						// offsets, nrml will be written
		//	_GAmplitude,												// amplitude
		//	_GFrequency,												// frequency
		//	_GSteepness,												// steepness
		//	_GSpeed,													// speed
		//	_GDirectionAB,												// direction # 1, 2
		//	_GDirectionCD												// direction # 3, 4
		//);
		//!!! /REMOVED
		
		//!!! INSERTED
		OceanParticleDisplace(offsets, nrml, v.texcoord);
		//!!! /INSERTED
		
		v.vertex.xyz += offsets;
```


## LICENSE

All texture images contained in this project are redistributed under [Creative Commons Attribution 4.0 International License](http://creativecommons.org/licenses/by/4.0/) from [Pixar One Twenty Eight by Pixar Animation Studios](https://community.renderman.pixar.com/article/114/library-pixar-one-twenty-eight.html).
![Creative Commons Attribution 4.0 International License](https://licensebuttons.net/l/by/4.0/88x31.png)

All else assets are licensed under [MIT license](http://opensource.org/licenses/MIT).
