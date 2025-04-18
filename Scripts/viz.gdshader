shader_type spatial;

uniform vec3 region_min = vec3(0.0, 0.0, 1.0);
uniform vec3 region_max = vec3(0.0, 0.0, -50.0);

uniform float type = 0.0;
uniform float length = 0.0;
uniform vec3 closestPoint;
uniform float color = 1;

void vertex() {
	vec3 val = (closestPoint - VERTEX) / length * type;
	COLOR = vec4(val.xyz, 1.0);
}

void fragment() {
	ALBEDO = vec3(COLOR.xyz);
}