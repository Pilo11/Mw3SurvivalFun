// FunExecuter Survival GSC: player tweaks.
// Body armor soaks fun_player_armor_health() instead of vanilla 250, and can be
// bought again while remaining armor is still above vanilla's 250 cutoff.
// Riot shield squads: up to fun_player_riot_max() per player.

fun_player_armor_health()
{
	return 1000;
}

fun_player_riot_max()
{
	return 2;
}

fun_player()
{
	if ( isdefined( level.fun_player ) )
		return;

	level.fun_player = 1;
	level endon( "special_op_terminated" );
	thread fun_player_setup_armory_loop();
	thread fun_player_wave1();

	for ( ;; )
	{
		if ( isdefined( level.players ) )
		{
			foreach ( player in level.players )
			{
				if ( isdefined( player ) && !isdefined( player.fun_player ) )
				{
					player.fun_player = 1;
					player thread fun_player_armor();
					player thread fun_player_riot_player();
					player thread fun_player_coords();
				}
			}
		}

		wait 0.25;
	}
}

fun_player_wave1()
{
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		level waittill( "wave_started", wave );

		if ( isdefined( wave ) && wave != 1 )
			continue;

		iprintlnbold( "Pilo's crazy survival fun" );
		return;
	}
}

fun_player_coord_elem( x, y )
{
	hud = newclienthudelem( self );
	hud.alignx = "left";
	hud.aligny = "bottom";
	hud.horzalign = "left";
	hud.vertalign = "bottom";
	hud.x = x;
	hud.y = y;
	hud.font = "hudbig";
	hud.fontscale = 0.45;
	hud.color = ( 1, 0.95, 0.7 );
	hud.alpha = 0.9;
	hud.foreground = 1;
	hud.hidewheninmenu = 1;
	hud.archived = 0;
	hud.sort = 20;
	return hud;
}

fun_player_coords()
{
	self endon( "disconnect" );
	level endon( "special_op_terminated" );
	lx = fun_player_coord_elem( 8, -16 );
	lx settext( "X:" );
	vx = fun_player_coord_elem( 28, -16 );
	ly = fun_player_coord_elem( 130, -16 );
	ly settext( "Y:" );
	vy = fun_player_coord_elem( 150, -16 );
	lz = fun_player_coord_elem( 252, -16 );
	lz settext( "Z:" );
	vz = fun_player_coord_elem( 272, -16 );

	for ( ;; )
	{
		if ( !isdefined( self ) )
			return;

		origin = self.origin;
		vx setvalue( int( origin[0] * 10000 ) * 0.0001 );
		vy setvalue( int( origin[1] * 10000 ) * 0.0001 );
		vz setvalue( int( origin[2] * 10000 ) * 0.0001 );
		wait 0.05;
	}
}

fun_player_setup_armory_loop()
{
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		fun_player_setup_armory();
		wait 0.25;
	}
}

fun_player_setup_armory()
{
	if ( !isdefined( level._id_189A ) )
		return 0;

	if ( isdefined( level._id_189A["armor"] ) )
	{
		level._id_189A["armor"]._id_3EC3 = ::fun_player_armor_allow;

		if ( !isdefined( level.fun_armor_give ) )
			level.fun_armor_give = level._id_189A["armor"]._id_3EC4;

		level._id_189A["armor"]._id_3EC4 = ::fun_player_armor_give;
	}

	if ( isdefined( level._id_189A["friendly_support_riotshield"] ) )
		level._id_189A["friendly_support_riotshield"]._id_3EC3 = ::fun_player_riot_allow;

	return isdefined( level._id_189A["armor"] );
}

fun_player_armor_allow( item )
{
	if ( !isdefined( self._id_3F16 ) || !isdefined( self._id_3F16["points"] ) )
		return 1;

	if ( self._id_3F16["points"] < fun_player_armor_health() )
		return 1;

	return 0;
}

fun_player_armor_give( item )
{
	if ( isdefined( level.fun_armor_give ) )
		self [[ level.fun_armor_give ]]( item );

	fun_player_armor_fill();
}

fun_player_armor_fill()
{
	if ( !isdefined( self._id_3F16 ) )
		return;

	self._id_3F16["type"] = "armor";
	self._id_3F16["points"] = fun_player_armor_health();

	if ( isdefined( self._id_3F19 ) )
		self._id_3F19 = fun_player_armor_health();

	self notify( "health_update" );
}

fun_player_armor()
{
	self endon( "death" );
	level endon( "special_op_terminated" );
	last = -1;

	for ( ;; )
	{
		if ( isdefined( self._id_3F16 ) && isdefined( self._id_3F16["type"] ) && self._id_3F16["type"] == "armor" && isdefined( self._id_3F16["points"] ) )
		{
			points = self._id_3F16["points"];

			if ( points == 250 && last < 250 )
			{
				fun_player_armor_fill();
				points = fun_player_armor_health();
			}

			last = points;
		}

		wait 0.05;
	}
}

fun_player_slot_free()
{
	icon = self getweaponhudiconoverride( "actionslot4" );

	if ( isdefined( icon ) && icon != "none" )
		return 0;

	if ( self hasweapon( "air_support_strobe" ) )
		return 0;

	return 1;
}

fun_player_riot_squads()
{
	if ( !isdefined( self.fun_riot_squads ) )
		self.fun_riot_squads = 0;

	return self.fun_riot_squads;
}

fun_player_riot_is_gign( guy )
{
	if ( !isdefined( guy ) || !isalive( guy ) )
		return 0;

	if ( isdefined( guy.headicon ) && guy.headicon == "headicon_gign_so" )
		return 1;

	if ( isdefined( guy._id_3D5D ) && isdefined( guy._id_3D5D._id_160B ) && issubstr( guy._id_3D5D._id_160B, "riotshield" ) )
		return 1;

	if ( isdefined( guy.aitype ) && issubstr( guy.aitype, "riot" ) )
		return 1;

	return 0;
}

fun_player_riot_allow( item )
{
	if ( !fun_player_slot_free() )
		return 0;

	if ( fun_player_riot_squads() >= fun_player_riot_max() )
		return 0;

	return 1;
}

fun_player_riot_player()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		new_members = [];
		allies = getaiarray( "allies" );

		foreach ( guy in allies )
		{
			if ( isdefined( guy.owner ) && guy.owner == self && fun_player_riot_is_gign( guy ) && !isdefined( guy.fun_riot ) )
				new_members[new_members.size] = guy;
		}

		if ( new_members.size > 0 )
		{
			wait 1;
			allies = getaiarray( "allies" );
			new_members = [];

			foreach ( guy in allies )
			{
				if ( isdefined( guy.owner ) && guy.owner == self && fun_player_riot_is_gign( guy ) && !isdefined( guy.fun_riot ) )
				{
					guy.fun_riot = 1;
					new_members[new_members.size] = guy;
				}
			}

			if ( new_members.size > 0 )
				self thread fun_player_riot_track_squad( new_members );
		}

		wait 0.5;
	}
}

fun_player_riot_track_squad( new_members )
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	if ( !isdefined( self.fun_riot_squads ) )
		self.fun_riot_squads = 0;

	self.fun_riot_squads++;
	self setweaponhudiconoverride( "actionslot4", "none" );
	monitor = spawn( "script_origin", ( 0, 0, 0 ) );

	foreach ( guy in new_members )
		guy thread fun_player_riot_member_wait( monitor );

	for ( i = 0; i < new_members.size; i++ )
		monitor waittill( "fun_riot_member_down" );

	monitor delete();

	if ( isdefined( self ) && isdefined( self.fun_riot_squads ) && self.fun_riot_squads > 0 )
		self.fun_riot_squads--;
}

fun_player_riot_member_wait( monitor )
{
	self waittill( "death" );

	if ( isdefined( monitor ) )
		monitor notify( "fun_riot_member_down" );
}
