struct OceanParticle {
	half3 position;
	half velocity;
	half force;
	int stopped;
};

uniform int XDivision;
uniform int ZDivision;
uniform int XCount;
uniform int ZCount;
uniform float XSize;
uniform float ZSize;
uniform StructuredBuffer<OceanParticle> Particles;

void OceanParticleDisplace(out half3 offsets, out half3 nrml, half2 texcoord) {
	nrml = half3(0,1,0);
	offsets = half3(0,0,0);
	
	int kj = texcoord.x * (float)XDivision;
	int ki = texcoord.y * (float)ZDivision;
	int kp = ki * XCount + kj;
	OceanParticle ptc = Particles[kp];
	OceanParticle ptcN = Particles[kp - (0<ki ? 1 : 0) * XCount];
	OceanParticle ptcS = Particles[kp + (ki<ZDivision ? 1 : 0) * XCount];
	OceanParticle ptcE = Particles[kp + (kj<XDivision ? 1 : 0)];
	OceanParticle ptcW = Particles[kp - (0<kj ? 1 : 0)];
	
	half dydx = (ptcE.position.y - ptcW.position.y) / 2;
	half dydz = (ptcS.position.y + ptcN.position.y) / 2;
	nrml = normalize(cross(half3(0,dydz,1), half3(1,dydx,0)));
	
	offsets.y = ptc.position.y;
}
