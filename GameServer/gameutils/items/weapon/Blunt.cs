/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections;
using System;
using System.Reflection;
using DOL.GS.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// Summary description for a Blunt
	/// </summary> 
	public class Blunt : Weapon
	{
		/// <summary>
		/// Gets or sets the damage type of this weapon
		/// </summary>
		public override eDamageType DamageType 
		{
			get
			{
				// Blunt can make damage of type : 
				// - legendary weapon : Matter, Spirit, Heat, Cold
				// - classic weapon : Crush
				if(m_damageType != eDamageType.Matter && m_damageType != eDamageType.Spirit && m_damageType != eDamageType.Heat && m_damageType != eDamageType.Cold)
					if(m_damageType != eDamageType.Crush) m_damageType = eDamageType.Crush;
				return m_damageType;
			}
			set
			{
				if ((DamageType == eDamageType.Matter) ||
				    (DamageType == eDamageType.Spirit) ||
				    (DamageType == eDamageType.Heat) ||
				    (DamageType == eDamageType.Cold) ||
				    (DamageType == eDamageType.Crush))
					m_damageType = DamageType;
			}
		}

		/// <summary>
		/// Gets how much hands are needed to use this weapon
		/// </summary>
		public override eHandNeeded HandNeeded 
		{
			get	// Blunt can only be right and left hand
			{
				if(m_handNeeded == eHandNeeded.TwoHands) m_handNeeded = eHandNeeded.RightHand;
				return m_handNeeded;
			}
			set
			{
				if(HandNeeded == eHandNeeded.TwoHands) m_handNeeded = eHandNeeded.RightHand;
					else m_handNeeded = HandNeeded;
			}
		}

		/// <summary>
		/// Gets all inventory slots where the item can be equipped
		/// </summary>
		public override eInventorySlot[] EquipableSlot 
		{
			get
			{
				if(m_handNeeded == eHandNeeded.RightHand)
					return new eInventorySlot[] {eInventorySlot.RightHandWeapon, eInventorySlot.TwoHandWeapon };
				else
					return new eInventorySlot[] {eInventorySlot.LeftHandWeapon, eInventorySlot.RightHandWeapon, eInventorySlot.TwoHandWeapon };
			}
		}

		/// <summary>
		/// Gets the object type of the template (for test use class type instead of this propriety)
		/// </summary>
		public override eObjectType ObjectType
		{
			get { return eObjectType.Blunt; }
		}
	}
}
