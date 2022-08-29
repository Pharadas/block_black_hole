#version 140
in vec2 position;

void main() {
    vec2 pos = position;
    // pos.x += t;
    // pos.y += t;
    gl_Position = vec4(pos, 0.0, 1.0);
}
