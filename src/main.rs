use std::fs;
use std::{env, path::PathBuf};
use device_query::{DeviceQuery, DeviceState, Keycode};
use glm::{vec2, vec3, TVec2, TVec3};

use std::vec;

extern crate nalgebra_glm as glm;

extern crate glium;

fn rotate3d(v: TVec3<f32>, rotation: TVec2<f32>) -> TVec3<f32> {
    let cos_a_x = rotation.y.cos();
    let sin_a_x = rotation.y.sin();

    let cos_a_y = rotation.x.cos();
    let sin_a_y = rotation.x.sin();

    let temp_vec = vec3(
        v.x,
        v.y * cos_a_x - v.z * sin_a_x,
        v.y * sin_a_x + v.z * cos_a_x
    );

    vec3(
        temp_vec.x * cos_a_y + temp_vec.z * sin_a_y,
        temp_vec.y,
        -temp_vec.x * sin_a_y + temp_vec.z * cos_a_y
    ) 
}

fn main() {
    use glium::{glutin, Surface, uniform, implement_vertex};

    let device_state = DeviceState::new();
    let current_folder: PathBuf;
    match env::current_exe() {
        Ok(exe_path) => {current_folder = exe_path.parent().unwrap().to_path_buf()},
        Err(e) => panic!("{}", e)
    }

    // * Load shaders
    let mut vertex_shader_path = current_folder.clone();
    vertex_shader_path.push("main.vs");

    let mut fragment_shader_path = current_folder.clone();
    fragment_shader_path.push("main.fs");
    // println!(vertex_shader_path)
    let vertex_shader = fs::read_to_string(vertex_shader_path).unwrap();
    let fragment_shader = fs::read_to_string(fragment_shader_path).unwrap();

    let mut player_position: TVec3<f32> = vec3(0., 0., 0.);
    let mut rot_direction: TVec2<f32> = vec2(0., 0.);

    let window_dimensions: TVec2<f32> = vec2(500., 500.);

    #[derive(Copy, Clone)]
    struct Vertex {
        position: [f32; 2]
    }

    implement_vertex!(Vertex, position);

    // * Initialize window and main loop
    let event_loops = glium::glutin::event_loop::EventLoop::new();
    let wb = glium::glutin::window::WindowBuilder::new()
        .with_inner_size(glium::glutin::dpi::LogicalSize::new(window_dimensions.x, window_dimensions.y))
        .with_title("Gaming time");
    let cb = glium::glutin::ContextBuilder::new();
    let display = glium::Display::new(wb, cb, &event_loops).unwrap();

    // * Initialize vertices and shaders
    let vertex1 = Vertex{position: [-1.0, -1.0]};
    let vertex2 = Vertex{position: [3.0, -1.0]};
    let vertex3 = Vertex{position: [-1.0, 3.0]};

    let shape = vec![vertex1, vertex2, vertex3];
    let vertex_buffer = glium::VertexBuffer::new(&display, &shape).unwrap();
    let indices = glium::index::NoIndices(glium::index::PrimitiveType::TrianglesList);

    let program = glium::Program::from_source(&display, &vertex_shader, &fragment_shader, None).unwrap();       

    // let mut t: f32 = 0.0;
    // let mut last_frame_time = std::time::Instant::now();
    let mut t: f32 = 0.;
    let mut delta: f32 = 0.;

    // * Run main loop
    event_loops.run(move |ev, _, control_flow| {
        let keys_pressed: Vec<Keycode> = device_state.get_keys();
        let current_frame_time = std::time::Instant::now();

        let mut relative_vel: TVec3<f32> = vec3(0., 0., 0.);

        let speed = 2.5;

        for key in keys_pressed.iter() {
            match key {
                &Keycode::W => {
                    relative_vel.z += speed * delta;
                },
                &Keycode::S => {
                    relative_vel.z -= speed * delta;
                },
                &Keycode::D => {
                    relative_vel.x += speed * delta;
                },
                &Keycode::A => {
                    relative_vel.x -= speed * delta;
                },
                &Keycode::Left => {
                    rot_direction.x -= 2.5 * delta;
                },
                &Keycode::Right => {
                    rot_direction.x += 2.5 * delta;
                },
                &Keycode::Up => {
                    rot_direction.y -= 2.5 * delta;
                },
                &Keycode::Down => {
                    rot_direction.y += 2.5 * delta;
                },

                _ => {}
            }
        }

        // *control_flow = glutin::event_loop::ControlFlow::WaitUntil(std::time::Instant::now() + std::time::Duration::from_secs_f32(1. / 60.));

        let mut target = display.draw();
        target.clear_color(0.5, 0.5, 0.5, 1.0);

        target.draw(&vertex_buffer,
                    &indices,
                    &program,
                    &uniform!{
                        t:t,
                        viewportDimensions: [window_dimensions.x, window_dimensions.y],
                        position: [player_position.x, player_position.y, player_position.z],
                        rotation: [rot_direction.x, rot_direction.y]
                    }, &Default::default()).unwrap();

        target.finish().unwrap();

        relative_vel = rotate3d(relative_vel, rot_direction);
        player_position += relative_vel;

        delta = current_frame_time.elapsed().as_secs_f32();
        t += delta;
        print!("{}           \r", 1./delta);

        match ev {
            // * Se pidio que la ventana se cerrara normalmente
            glutin::event::Event::WindowEvent { event, .. } => match event {
                glutin::event::WindowEvent::CloseRequested => {
                    *control_flow = glutin::event_loop::ControlFlow::Exit;
                    return;
                },

                _ => return,
            },
            
            _ => (),
        }
    });
}
