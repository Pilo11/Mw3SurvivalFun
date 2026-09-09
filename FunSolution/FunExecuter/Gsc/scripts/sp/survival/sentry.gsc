// FunExecuter Survival GSC: sentry health, $500 minigun price, cap of 4.
// Team cap is fun_sentry_max(). In co-op each player is limited to fun_sentry_player_max().
// Armory tables live in a different ScriptFile, so price/cap are applied at runtime.

fun_sentry_max()
{
	return 4;
}

fun_sentry_player_max()
{
	return 2;
}

fun_sentry_price()
{
	return 500;
}

fun_sentry_is_coop()
{
	return isdefined( level.players ) && level.players.size > 1;
}

fun_sentry_slot_free()
{
	icon = self getweaponhudiconoverride( "actionslot4" );

	if ( isdefined( icon ) && icon != "none" )
		return 0;

	if ( self hasweapon( "air_support_strobe" ) )
		return 0;

	return 1;
}

fun_sentry_player_owned()
{
	count = 0;

	if ( _id_0611::_id_3CF4( "sentry" ) )
		count++;

	if ( _id_0611::_id_3CF4( "sentry_gl" ) )
		count++;

	if ( isdefined( level._id_3C67 ) )
	{
		foreach ( turret in level._id_3C67 )
		{
			if ( isdefined( turret ) && isdefined( turret.attacker ) && isplayer( turret.attacker ) && turret.attacker == self )
				count++;
		}
	}

	return count;
}

fun_sentry_owned_total()
{
	count = 0;

	if ( isdefined( level.players ) )
	{
		foreach ( player in level.players )
		{
			if ( isdefined( player ) )
				count += player fun_sentry_player_owned();
		}
	}

	return count;
}

fun_sentry_allow( item )
{
	if ( !fun_sentry_slot_free() )
		return 0;

	if ( fun_sentry_owned_total() >= fun_sentry_max() )
		return 0;

	if ( fun_sentry_is_coop() && fun_sentry_player_owned() >= fun_sentry_player_max() )
		return 0;

	return 1;
}

fun_sentry_setup_armory()
{
	if ( !isdefined( level._id_189A ) )
		return 0;

	if ( isdefined( level._id_189A["sentry"] ) )
	{
		level._id_189A["sentry"]._id_3EC1 = fun_sentry_price();
		level._id_189A["sentry"]._id_3EC3 = ::fun_sentry_allow;
	}

	if ( isdefined( level._id_189A["sentry_gl"] ) )
		level._id_189A["sentry_gl"]._id_3EC3 = ::fun_sentry_allow;

	return isdefined( level._id_189A["sentry"] );
}

fun_sentry()
{
	if ( isdefined( level.fun_sentry ) )
		return;

	level.fun_sentry = 1;
	level endon( "special_op_terminated" );
	thread fun_sentry_setup_armory_loop();

	for ( ;; )
	{
		if ( isdefined( level.players ) )
		{
			foreach ( player in level.players )
			{
				if ( isdefined( player ) && !isdefined( player.fun_sentry ) )
				{
					player.fun_sentry = 1;
					player thread fun_sentry_player();
				}
			}
		}

		turrets = getentarray( "misc_turret", "classname" );
		foreach ( turret in turrets )
			turret thread fun_sentry_keep();

		wait 0.25;
	}
}

fun_sentry_setup_armory_loop()
{
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		fun_sentry_setup_armory();
		wait 0.25;
	}
}

fun_sentry_player()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		self waittill( "new_sentry", sentry );

		if ( isdefined( sentry ) )
			sentry thread fun_sentry_keep();
	}
}

fun_sentry_keep()
{
	if ( !isdefined( self ) || isdefined( self.fun_sentry ) )
		return;

	self.fun_sentry = 1;
	self endon( "death" );
	level endon( "special_op_terminated" );
	thread fun_sentry_on_damage();

	for ( ;; )
	{
		fun_sentry_restore();
		wait 0.05;
	}
}

fun_sentry_on_damage()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		self waittill( "damage" );
		fun_sentry_restore();
	}
}

fun_sentry_restore()
{
	if ( !isdefined( self ) )
		return;

	self.maxhealth = 999999;
	self.health = 999999;

	if ( isdefined( self.currenthealth ) )
		self.currenthealth = self.health;

	if ( isdefined( self.healthbuffer ) )
		self.healthbuffer = 0;
}
