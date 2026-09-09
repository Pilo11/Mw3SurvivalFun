// FunExecuter Survival GSC: keep placed sentries alive.
// Replaces the old in-memory health-subtraction skip (sentry health 20800).

fun_sentry_health()
{
	if ( isdefined( level.fun_sentry_health ) )
		return;

	level.fun_sentry_health = 1;
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		if ( isdefined( level.players ) )
		{
			foreach ( player in level.players )
			{
				if ( isdefined( player ) && !isdefined( player.fun_sentry_health ) )
				{
					player.fun_sentry_health = 1;
					player thread fun_sentry_health_player();
				}
			}
		}

		turrets = getentarray( "misc_turret", "classname" );
		foreach ( turret in turrets )
			turret thread fun_sentry_health_keep();

		wait 0.25;
	}
}

fun_sentry_health_player()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		self waittill( "new_sentry", sentry );

		if ( isdefined( sentry ) )
			sentry thread fun_sentry_health_keep();
	}
}

fun_sentry_health_keep()
{
	if ( !isdefined( self ) || isdefined( self.fun_sentry_health ) )
		return;

	self.fun_sentry_health = 1;
	self endon( "death" );
	level endon( "special_op_terminated" );
	thread fun_sentry_health_on_damage();

	for ( ;; )
	{
		fun_sentry_health_restore();
		wait 0.05;
	}
}

fun_sentry_health_on_damage()
{
	self endon( "death" );
	level endon( "special_op_terminated" );

	for ( ;; )
	{
		self waittill( "damage" );
		fun_sentry_health_restore();
	}
}

fun_sentry_health_restore()
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
