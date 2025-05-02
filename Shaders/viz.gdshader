shader_type spatial;

instance uniform vec3 color = vec3(165, 42, 42);

void vertex() {
	COLOR = vec4(color.xyz, 1.0);
}

void fragment() {
	ALBEDO = COLOR.xyz;
}