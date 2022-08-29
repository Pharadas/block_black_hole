#version 460
out vec4 color;
uniform vec2 viewportDimensions;
uniform float t;
uniform vec3 position;
uniform vec2 rotation;

#define PI 3.1415926

#define MAX_REHITS 1

const bool USE_BRANCHLESS_DDA = true;
const int MAX_RAY_STEPS = 128;

float sdSphere(vec3 p, float d) { return length(p) - d; } 

float sdBox( vec3 p, vec3 b ) {
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) +
         length(max(d,0.0));
}

bool getVoxel(ivec3 c) {
	// return c == vec3(0, 0, 0) || c == vec3(0, 1, 2) || c == vec3(-2, 0, 0) || c == vec3(0, 1, 0);
	// mat2 R = mat2(vec2(cos(t), sin(t)), vec2(-sin(t), cos(t)));
	// R *= 5;
	// ivec3 movingCube = ivec3(5, floor(sin(t * 5)), 5);
	// movingCube.xz = ivec2(floor(R * movingCube.xz));
	// float changer = sin(t * 10);

	return c == vec3(0, 0, 0) || c == vec3(-1, 0, 1) || c == vec3(-1, -1, 0) || c == vec3(1, 1, 0) || c == vec3(0, 2, 0) || c == vec3(10, 10, 10) || c == vec3(5, 5, 5) || c.y == int(sin((c.x * c.z) / 100 * (t * 0.1)) * 2.5);
//  c.y == -2 ||

	// int f = 15;
	// return c.x % f == 0 || c.y % f == 0 || c.z % f == 0;

	// vec3 movinCube = vec3(10 * cos(t / 100), 10 * sin(t / 100), 5);

	// return c.y == -2 || c == vec3(-1, -1, -1) || c == vec3(1, 1, -1) || c == vec3(0, -1, -1) || c == vec3(-1, -1, 0) || c == vec3(0, -1, 0) || (c.z == 5. && c.x == 5.);

	// return c.y == floor(sin((c.x * 10) + (c.z * 10)));
}

// bool getVoxel(ivec3 c) {
// 	vec3 p = vec3(c) + vec3(0.5);
// 	float d = min(max(-sdSphere(p, 7.5), sdBox(p, vec3(6.0))), -sdSphere(p, 25.0));
// 	return d < 0.0;
// }

float checker(vec3 p) {
    return step(0.0, sin(PI * p.x + PI/2.0)*sin(PI *p.y + PI/2.0)*sin(PI *p.z + PI/2.0));
}

vec3 rotate3dZ(vec3 v, float a) {
    float cosA = cos(a);
    float sinA = sin(a);
    return vec3(
        v.x * cosA - v.y * sinA,
        v.x * sinA + v.y * cosA,
        v.z);
}

vec3 rotate3dY(vec3 v, float a) {
    float cosA = cos(a);
    float sinA = sin(a);
    return vec3(
        v.x * cosA + v.z * sinA,
        v.y,
        -v.x * sinA + v.z * cosA
    );
}

vec3 rotate3dX(vec3 v, float a) {
    float cosA = cos(a);
    float sinA = sin(a);
    return vec3(
        v.x,
        v.y * cosA - v.z * sinA,
        v.y * sinA + v.z * cosA
    );
}

vec2 rotate2d(vec2 v, float a) {
	float sinA = sin(a);
	float cosA = cos(a);
	return vec2(v.x * cosA - v.y * sinA, v.y * cosA + v.x * sinA);	
}

void main() {
    // vec4 fragCoord = gl_FragCoord;
    vec4 fragCoord = gl_FragCoord;
	vec2 screenPos = (fragCoord.xy / viewportDimensions.xy) * 2.0 - 1.0;
	vec3 cameraDir = vec3(0.0, 0.0, 0.8);
	vec3 cameraPlaneU = vec3(1.0, 0.0, 0.0);
	vec3 cameraPlaneV = vec3(0.0, 1.0, 0.0) * viewportDimensions.y / viewportDimensions.x;
	vec3 rayDir = cameraDir + screenPos.x * cameraPlaneU + screenPos.y * cameraPlaneV;
	vec3 rayPos = position;
	vec3 realPos = position;

    rayDir = rotate3dX(rayDir, rotation.y);
    rayDir = rotate3dY(rayDir, rotation.x);

	rayDir = normalize(rayDir);

    int hits = 0;

	float r = 10;
	vec3 lightPosition = vec3(r * sin(t), 10, r * cos(t));

	ivec3 mapPos = ivec3(floor(rayPos + 0.));

	// vec3 deltaDist = abs(vec3(length(rayDir)) / rayDir);
	vec3 deltaDist = 1.0 / abs(rayDir);
	ivec3 rayStep = ivec3(sign(rayDir));
	vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist; 

	vec3 lastDeltaDist;
	ivec3 lastRayStep;
	ivec3 lastMapPos;
	vec3 lastSideDist;
	bvec3 lastMask;

	// vec3 deltaDist = 1.0 / abs(rayDir);
	// ivec3 rayStep = ivec3(sign(rayDir));
	// vec3 sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

	vec3 lastHitPosition;
	bvec3 mask;

	float d;
	vec3 dst;
	vec3 originalDir = rayDir;

	color = vec4(1.);
	// vec3 blackHolePosition = vec3(5, int(t) % 10, 5);
	vec3 blackHolePosition = vec3(0, 5, 0);

	for (int i = 0; i < MAX_RAY_STEPS; i++) {
		if (getVoxel(mapPos) || mapPos == ivec3(lightPosition)) {
			hits++;

			d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
			dst = rayPos + rayDir * d; 

			lastHitPosition = dst;

			// float t = 0.5 + 0.5 * checker(dst);
			float t = .2 + checker(dst);

			if (mask.x) {
				color *= vec4(t, t, t, 1);

				color *= (vec4(0.8, 0.1, 0.1, 1.0) * vec4(1. / float(hits)));
				// if (hits != MAX_REHITS) rayDir.x *= -1;
				rayDir.x *= -1;
			}

			if (mask.y) {
				color *= vec4(t, t, t, 1);

				color *= (vec4(0.1, 0.1, 0.8, 1.0) * vec4(1. / float(hits)));
				// if (hits != MAX_REHITS) rayDir.y *= -1;
				rayDir.y *= -1;
			}

			if (mask.z) {
				color *= vec4(t, t, t, 1);

				color *= (vec4(0.1, 0.8, 0.1, 1.0) * vec4(1. / float(hits)));
				// if (hits != MAX_REHITS) rayDir.z *= -1;
				rayDir.z *= -1;
			}

			if (hits > MAX_REHITS) break;

			mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));

			deltaDist = abs(vec3(length(rayDir)) / rayDir);
			rayStep = ivec3(sign(rayDir));
			sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

			lastMapPos = mapPos;

			lastMask = mask;

			lastDeltaDist = deltaDist;
			lastRayStep = rayStep;
			lastSideDist = sideDist;
		}

		mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));

		sideDist += vec3(mask) * deltaDist;
		mapPos += ivec3(vec3(mask)) * rayStep;

		d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
		dst = rayPos + rayDir * d; 

		vec3 newDir = blackHolePosition - dst;

		float force = .1 / pow(length(newDir), 3);

		if (dot(newDir * force, rayDir) > 0.1) {
			color = vec4(0.);
			break;
		}

		rayDir = normalize(rayDir + (newDir * force));
		deltaDist = abs(vec3(length(rayDir)) / rayDir);
		rayStep = ivec3(sign(rayDir));
		sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;
	}

	// if (hits < MAX_REHITS + 1) {
	// 	color = vec4(0.);
	// }

	// color *= mask.x ? vec4(1., 0.1, 0.1, 1.) : mask.y ? vec4(0.1, 0.1, 1.0, 1.0) : mask.z ? vec4(0.1, 1.0, 0.1, 1.0) : vec4(0.0);

	// mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));

	// sideDist += vec3(mask) * deltaDist;
	// mapPos += ivec3(vec3(mask)) * rayStep;

	// if (hits != 0) {
	// 	rayPos = lastHitPosition;

	// 	// * Ahora dispararle a la luz y ver si choca con algo antes
	// 	rayDir = normalize(vec3(10, 10, 10) - lastHitPosition);
	// }

	// rayPos = dst;

	// rayDir = normalize(vec3(10, 10, 10) - dst);

	rayPos = lastHitPosition;
	rayDir = normalize(lightPosition - lastHitPosition);

	// TODO: Primero checar que el lado en el que estemos este expuesto a la luz (algo como if (luz.x > pos.x && mask.x) break;)

	if (hits < MAX_REHITS + 1) {
		deltaDist = lastDeltaDist;
		rayStep = lastRayStep;
		sideDist = lastSideDist;
		mask = lastMask;
		mapPos = lastMapPos;
		// color = vec4(0.);
		deltaDist = abs(vec3(length(rayDir)) / rayDir);
		rayStep = ivec3(sign(rayDir));
		sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

	} else {
		deltaDist = abs(vec3(length(rayDir)) / rayDir);
		rayStep = ivec3(sign(rayDir));
		sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;
	}

	for (int i = 0; i < MAX_RAY_STEPS; i++) {
		mask = lessThanEqual(sideDist.xyz, min(sideDist.yzx, sideDist.zxy));

		sideDist += vec3(mask) * deltaDist;
		mapPos += ivec3(vec3(mask)) * rayStep;

		// d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
		// dst = rayPos + rayDir * d; 

		// vec3 newDir = blackHolePosition - dst;

		// float force = .1 / pow(length(newDir), 3);

		// if (dot(newDir * force, rayDir) > 0.1) {
		// 	color = vec4(0.);
		// 	break;
		// }

		// rayDir = normalize(rayDir + (newDir * force));
		// deltaDist = abs(vec3(length(rayDir)) / rayDir);
		// sideDist = (sign(rayDir) * (vec3(mapPos) - rayPos) + (sign(rayDir) * 0.5) + 0.5) * deltaDist;

		if (getVoxel(mapPos)) {
			if (mapPos == ivec3(lightPosition)) {
				color *= vec4(0.1, 1.0, 0.1, 1.0);
				break;
			} else {
				// color *= vec4(0.8824, 0.0, 1.0, 0.0);
				color *= vec4(0.1);
				break;
			}
		}
	}

	// float d = length(vec3(mask) * (sideDist - deltaDist)); // rayDir normalized
	// vec3 dst = rayPos + rayDir * d; 

	// float t = 0.5 + 0.5 * checker(dst);
	// color *= vec4(t, t, t, 1);

    // vec4 fogcolor = vec4(0.25, 0.4, 0.5, 1);
    //vec3 fogcolor = vec3(0.75, 0.6, 0.3); // smog
    // color *= mix(fogcolor, color, exp(-d * d / 200.0)); // fog for depth impression & to suppress flickering

	if (hits == 0) color = vec4(0.);
	// else if (hits < MAX_REHITS) {
	// 	color *= vec4(0.1);
	// }
}