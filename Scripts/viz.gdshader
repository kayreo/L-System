shader_type spatial;

uniform vec3 region_min = vec3(0.0, 0.0, 1.0);
uniform vec3 region_max = vec3(0.0, 0.0, -50.0);

void fragment() {
    if (
        VERTEX.x > region_min.x && VERTEX.x < region_max.x &&
        VERTEX.y > region_min.y && VERTEX.y < region_max.y &&
        VERTEX.z > region_min.z && VERTEX.z < region_max.z
    ) {
        ALBEDO = vec3(1.0, 0.0, 0.0); // Inside region
    } else {
        ALBEDO = vec3(0.0, 0.0, 1.0); // Outside
    }
}