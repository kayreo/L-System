shader_type spatial;

uniform vec3 region_min = vec3(0.0, 0.0, 1.0);
uniform vec3 region_max = vec3(0.0, 0.0, -50.0);

uniform int type = 0;
uniform float color = 1;

void vertex() {

	COLOR = vec4(0.0, 0.0, 0.0, 1.0);
}

void fragment() {
	//
    if (type == 1) {
        ALBEDO = vec3(COLOR.xyz); //vec3(VERTEX.x, VERTEX.y, VERTEX.z);
    } else if (type == 2) {
		ALBEDO =vec3(COLOR.xyz);
    } else {
		ALBEDO = vec3(COLOR.xyz);
	}
}