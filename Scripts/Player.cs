/**
 * Description:  Player Controller
 * Author:       Alicia Amarilla (smushyaa@gmail.com)
 * File Created: October 05, 2023
*/
using Godot;

struct PlayerInput {
    public float move_horizontal;
    public bool  is_moving;
    public bool  is_jump_press;
    public bool  is_jump_hold;
    public bool  is_run_hold;
};

public partial class Player : RigidBody2D {

    public bool is_grounded { get; private set; }

    [ExportGroup("Physics")]
    /// Speed at which player accelerates when being controlled.
    [Export]
    float acceleration_speed = 250.0f;
    /// Scales base acceleration when player is running.
    [Export]
    float acceleration_run_scale = 1.5f;
    /// Scales base acceleration when player is in the air.
    [Export(PropertyHint.Range, "0.0,1.0")]
    float acceleration_aerial_scale = 0.25f;
    /// Maximum allowed walk velocity.
    [Export]
    float max_walk_velocity = 300.0f;
    /// Maximum allowed run velocity.
    [Export]
    float max_run_velocity = 600.0f;
    /// Drag applied when player stops moving on ground.
    /// No drag is applied in the air.
    /// Only affects X-axis velocity.
    [Export]
    float stop_drag = 15.0f;
    /// Force of impulse applied when player jumps.
    [Export]
    float jump_force = 300.0f;
    /// Gravity scale when jump is held and moving up.
    [Export]
    float jump_gravity_scale = 1.0f;
    /// Normal gravity scale.
    [Export]
    float fall_gravity_scale = 3.5f;

    PlayerInput input;
    Vector2 acceleration;

    float max_velocity;
    const float VELOCITY_TRANSITION_SPEED = 10.0f;

    float drag = 0.0f;

    // NOTE(alicia): this feels wrong lol
    float delta_time;

    RayCast2D ray_center = null;
    RayCast2D ray_left   = null;
    RayCast2D ray_right  = null;

    public override void _Ready() {
        acceleration = Vector2.Zero;

        ray_center = GetNode<RayCast2D>( "GroundCheckCenter" );
        ray_left   = GetNode<RayCast2D>( "GroundCheckLeft" );
        ray_right  = GetNode<RayCast2D>( "GroundCheckRight" );

        max_velocity = max_walk_velocity;
    }

    PlayerInput process_input() {
        PlayerInput result;

        result.move_horizontal = Input.GetAxis( "Left", "Right" );
        result.is_moving       = Mathf.Abs( result.move_horizontal ) > 0.001f;
        result.is_jump_press   = Input.IsActionJustPressed( "Jump" );
        result.is_jump_hold    = Input.IsActionPressed( "Jump" );
        result.is_run_hold     = Input.IsActionPressed( "Run" );

        return result;
    }

    public override void _Process( double dt ) {
        delta_time = (float)dt;

        input = process_input();
        float acc = acceleration_speed;
        if( is_grounded ) {
            if( input.is_run_hold ) {
                acc *= acceleration_run_scale;
            }
        } else {
            acc *= acceleration_aerial_scale;
        }
        acceleration = new Vector2( input.move_horizontal * acc, 0.0f );

        if( is_grounded && input.is_jump_press ) {
            ApplyImpulse( Vector2.Up * jump_force );
        }

        if( is_grounded ) {
            GravityScale = 0.0f;
        } else {
            if( input.is_jump_hold && LinearVelocity.Y < 0.0f ) {
                GravityScale = jump_gravity_scale;
            } else {
                GravityScale = fall_gravity_scale;
            }
        }

        max_velocity = Mathf.Lerp(
            max_velocity,
            input.is_run_hold ? max_run_velocity : max_walk_velocity,
            delta_time * VELOCITY_TRANSITION_SPEED );
    }

    public override void _PhysicsProcess( double dt ) {
        delta_time = (float)dt;

        if( is_grounded ) {
            if( input.is_moving ) {
                drag = 0.0f;
            } else {
                drag = stop_drag;
            }
        } else {
            drag = 0.0f;
        }

        ApplyForce( acceleration );

        is_grounded =
            ray_center.IsColliding() ||
            ray_left.IsColliding()   ||
            ray_right.IsColliding();
    }

    public override void _IntegrateForces( PhysicsDirectBodyState2D state ) {
        float vx = state.LinearVelocity.X;
        float vy = state.LinearVelocity.Y;

        if( is_grounded ) {
            vx = Mathf.Clamp( vx, -max_velocity, max_velocity );
        }
        vx = vx * ( 1.0f - delta_time * drag );

        state.LinearVelocity = new Vector2( vx, vy );
    }

};

