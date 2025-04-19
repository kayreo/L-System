shader_type spatial;

instance uniform vec3 color;

// start of entire curve
// value = point - start / length
// color = vec3(value)
void vertex() {
	COLOR = vec4(color.xyz, 1.0);
}

void fragment() {
	ALBEDO = vec3(COLOR.xyz);
}