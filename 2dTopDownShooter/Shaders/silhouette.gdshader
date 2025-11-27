shader_type canvas_item;

// Silhouette shader - renders the sprite as a semi-transparent dark outline
// Used for showing player position when behind buildings

uniform vec4 silhouette_color : source_color = vec4(0.0, 0.0, 0.3, 0.5);

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	if (tex.a > 0.1) {
		COLOR = vec4(silhouette_color.rgb, tex.a * silhouette_color.a);
	} else {
		COLOR = vec4(0.0);
	}
}
