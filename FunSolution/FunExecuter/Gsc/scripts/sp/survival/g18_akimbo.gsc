// FunExecuter Survival GSC: turn the existing Machine Pistols G18 slot into
// akimbo G18 labeled "PUFF PUFF". The weapon menu is hardcoded to CSV rows, so a
// new item never appears — this reuses iw5_g18_mp (row 7, machinepistol).

fun_g18_akimbo_item_name( index )
{
	if ( index == 7 || index == "7" )
		return "iw5_g18_mp";

	return "";
}

fun_g18_akimbo_register()
{
	if ( isdefined( level.fun_g18_akimbo ) )
		return;

	item = undefined;

	if ( isdefined( level._id_3D68 ) && isdefined( level._id_3D68["weapon"] ) )
		item = level._id_3D68["weapon"]["iw5_g18_mp"];

	if ( !isdefined( item ) && isdefined( level._id_189A ) )
		item = level._id_189A["iw5_g18_mp"];

	if ( !isdefined( item ) )
		return;

	level.fun_g18_akimbo = 1;
	precacheitem( "iw5_g18_mp_akimbo" );

	item.name = "PUFF PUFF";
	item._id_189B = "PUFF PUFF";
	item._id_160B = "iw5_g18_mp";
	item.type = "weapon";
	item._id_3EC0 = "machinepistol";
	item._id_3EC1 = 10;
	item._id_3EC2 = 0;
	item._id_3EC3 = ::fun_g18_akimbo_can_buy;
	item._id_3EC4 = ::fun_g18_akimbo_give;
}

fun_g18_akimbo_can_buy( item_name )
{
	weapons = self getweaponslistprimaries();

	foreach ( weapon in weapons )
	{
		if ( issubstr( weapon, "iw5_g18_mp" ) )
			return 0;
	}

	return 1;
}

fun_g18_akimbo_give( item_name )
{
	weapon = "iw5_g18_mp_akimbo";
	primaries = self getweaponslistprimaries();

	if ( primaries.size > 1 )
	{
		current = self getcurrentweapon();

		if ( isdefined( current ) && current != "none" )
			self takeweapon( current );
	}

	self giveweapon( weapon );
	self setweaponammoclip( weapon, weaponclipsize( weapon ) );
	self setweaponammostock( weapon, weaponmaxammo( weapon ) );
	self switchtoweapon( weapon );
}
