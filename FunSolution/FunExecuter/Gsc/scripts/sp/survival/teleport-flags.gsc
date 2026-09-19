// FunExecuter Survival GSC: two flags on Terminal that teleport to each other.
// One sits on the plane roof; the other is behind the body scanners in the
// airport security hall. Landings are offset so they cannot loop.
// Do not define init()/main() — host scripts such as 1571 already use those names.

fun_teleport_flags()
{
	if ( isdefined( level.fun_teleport_flags ) )
		return;

	level.fun_teleport_flags = 1;
	level endon( "special_op_terminated" );

	if ( !fun_teleport_flags_is_terminal() )
		return;

	precachemodel( "prop_flag_neutral" );
	wait 1;
	fun_teleport_flags_spawn();
}

fun_teleport_flags_is_terminal()
{
	if ( isdefined( level.script ) && issubstr( level.script, "terminal" ) )
		return 1;

	return issubstr( getdvar( "mapname" ), "terminal" );
}

fun_teleport_flags_trace( x, y, z_start, z_end )
{
	trace = bullettrace( ( x, y, z_start ), ( x, y, z_end ), 0, undefined );

	if ( isdefined( trace ) && isdefined( trace["fraction"] ) && trace["fraction"] < 1 && isdefined( trace["position"] ) )
		return trace["position"];

	return undefined;
}

fun_teleport_flags_spawn()
{
	plane = ( 608, 3557, 360 );

	scanner = fun_teleport_flags_trace( 250, 4900, 280, 140 );

	if ( !isdefined( scanner ) )
		scanner = ( 250, 4900, 192 );

	plane_flag = plane + ( 0, 0, 2 );
	scanner_flag = scanner + ( 0, 0, 2 );
	plane_land = ( plane[0], plane[1] - 90, plane[2] + 8 );
	scanner_land = ( scanner[0] + 90, scanner[1], scanner[2] + 8 );
	plane_trig = fun_teleport_flags_make( plane_flag, ( 0, 90, 0 ) );
	scanner_trig = fun_teleport_flags_make( scanner_flag, ( 0, 0, 0 ) );
	plane_trig.fun_land = scanner_land;
	plane_trig.fun_look = ( 0, 0, 0 );
	plane_trig.fun_dest = scanner_trig;
	scanner_trig.fun_land = plane_land;
	scanner_trig.fun_look = ( 0, 180, 0 );
	scanner_trig.fun_dest = plane_trig;
	plane_trig thread fun_teleport_flags_think();
	scanner_trig thread fun_teleport_flags_think();
}

fun_teleport_flags_make( origin, angles )
{
	flag = spawn( "script_model", origin );
	flag setmodel( "prop_flag_neutral" );
	flag.angles = angles;
	trig = spawn( "trigger_radius", origin - ( 0, 0, 16 ), 0, 48, 96 );
	trig.fun_flag = flag;
	return trig;
}

fun_teleport_flags_think()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		self waittill( "trigger", player );

		if ( !isdefined( player ) || !isplayer( player ) || !isalive( player ) )
			continue;

		if ( isdefined( player.fun_teleport_flags_busy ) && player.fun_teleport_flags_busy )
			continue;

		if ( !isdefined( self.fun_land ) )
			continue;

		player thread fun_teleport_flags_send( self );
	}
}

fun_teleport_flags_send( src )
{
	self endon( "death" );
	level endon( "special_op_terminated" );
	self.fun_teleport_flags_busy = 1;
	self enableinvulnerability();
	self setorigin( src.fun_land );

	if ( isdefined( src.fun_look ) )
		self setplayerangles( src.fun_look );

	if ( isdefined( self.maxhealth ) )
		self.health = self.maxhealth;

	wait 0.4;
	dest = src.fun_dest;

	if ( isdefined( dest ) )
	{
		for ( i = 0; i < 40; i++ )
		{
			if ( !isdefined( self ) || !isalive( self ) )
				break;

			if ( !self istouching( dest ) && !self istouching( src ) )
				break;

			wait 0.05;
		}
	}
	else
		wait 1;

	if ( isdefined( self ) )
	{
		self disableinvulnerability();
		self.fun_teleport_flags_busy = undefined;
	}
}
