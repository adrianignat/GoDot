shader_type canvas_item;

// Ring/outline indicator for showing player position when behind objects
uniform vec4 indicator_color : source_color = vec4(1.0, 1.0, 1.0, 0.6);
uniform float width : hint_range(0.1, 1.0) = 0.9;
uniform float height : hint_range(0.1, 1.0) = 0.5;
uniform float ring_thickness : hint_range(0.01, 0.3) = 0.08;

void fragment() {
	vec2 center = vec2(0.5, 0.5);
	vec2 uv_centered = UV - center;

	// Ellipse equation: (x/a)^2 + (y/b)^2
	float a = width * 0.5;
	float b = height * 0.5;
	float ellipse = (uv_centered.x * uv_centered.x) / (a * a) + (uv_centered.y * uv_centered.y) / (b * b);

	// Create ring by checking if we're between inner and outer ellipse
	float inner = 1.0 - ring_thickness;
	float outer = 1.0;

	if (ellipse >= inner && ellipse <= outer) {
		// Soft edges on both sides
		float edge_softness = 0.15;
		float alpha_outer = smoothstep(outer, outer - edge_softness, ellipse);
		float alpha_inner = smoothstep(inner, inner + edge_softness, ellipse);
		float alpha = alpha_outer * alpha_inner;
		COLOR = vec4(indicator_color.rgb, indicator_color.a * alpha);
	} else {
		COLOR = vec4(0.0);
	}
}
