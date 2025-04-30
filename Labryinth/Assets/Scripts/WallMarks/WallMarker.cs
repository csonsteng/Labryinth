using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMarker : MonoBehaviour
{

	// todo hangs onto the mesh of the marker
	// is an interactable
	// on interaction, the material gets updated to the next material in the list
	// each material will be a different mark
	// hmm. maybe just update UVs and keep all in one material??

	// also set up a canvas pooler for interactables, so we don't have to retain one canvas per mark.
	// in most cases there will only ever be 0-1 active interactables.
	// never mind. can only have 1. So no need to pool. Just update the transform rather than giving each its own
}
