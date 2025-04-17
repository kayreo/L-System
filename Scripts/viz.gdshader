shader_type spatial;

uniform vec3 region_min = vec3(0.0, 0.0, 1.0);
uniform vec3 region_max = vec3(0.0, 0.0, -50.0);

uniform int type = 0;


void fragment() {
    if (type == 1) {
        ALBEDO = vec3(1.0, 0.0, 0.0); // Inside region
    } else if (type == 2) {
        ALBEDO = vec3(1.0, 0.0, 1.0); // Outside
    } else {
		ALBEDO = vec3(0.0, 0.0, 0.0);
	}
}