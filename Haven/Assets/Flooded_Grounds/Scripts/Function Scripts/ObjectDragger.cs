using UnityEngine;

public class ObjectDragger : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragDistance = 5f;           // How far away you can drag objects
    public float dragSpeed = 50f;              // How fast objects follow the cursor (increased for responsiveness)
    public float maxDragDistance = 10f;        // Maximum distance object can be dragged from player
    public LayerMask draggableLayer = ~0;      // Layer mask for draggable objects (defaults to everything)
    public bool requireRigidbody = true;      // Whether objects need a Rigidbody to be dragged
    public bool useInstantDrag = false;       // If true, object follows cursor instantly (no lerp)
    public float minDragDistance = 2f;        // Minimum distance from camera when dragging
    
    [Header("Collision Detection")]
    public LayerMask obstacleLayer = ~0;       // Layer mask for obstacles (walls, etc.) that block dragging - set this to include ground/terrain
    public float collisionCheckRadius = 0.5f;   // Radius to check for collisions around the object
    public bool preventDragThroughWalls = true; // Whether to prevent dragging through solid objects
    public float collisionBuffer = 0.05f;        // Buffer distance to maintain from obstacles
    
    [Header("Visual Feedback")]
    public bool showDragPreview = true;        // Show visual feedback when dragging
    public Color dragHighlightColor = Color.yellow;
    
    private Camera playerCamera;
    private GameObject draggedObject;
    private Rigidbody draggedRigidbody;
    private Vector3 dragOffset;
    private float dragDistanceFromCamera;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 lastValidPosition; // Last position that didn't have collisions
    
    // Store original physics state
    private bool wasKinematic;
    private bool usedGravity;
    private float originalDrag;
    private float originalAngularDrag;
    private CollisionDetectionMode originalCollisionDetectionMode;
    
    // Collider bounds for collision checking
    private Bounds objectBounds;
    private Collider[] objectColliders;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("ObjectDragger: No main camera found!");
        }
    }
    
    void Update()
    {
        if (playerCamera == null) return;
        
        // Start dragging
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            TryStartDrag();
        }
        
        // Continue dragging
        if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDrag();
        }
        
        // Stop dragging
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            StopDrag();
        }
    }
    
    void TryStartDrag()
    {
        // Don't start dragging if player is trying to place something (bed, campfire, etc.)
        if (IsPlayerPlacingObject())
        {
            return;
        }
        
        // Check if player is holding a tool that can attack destroyable objects
        // If so, don't start dragging (let attack system handle it)
        if (IsPlayerHoldingAttackTool())
        {
            RaycastHit hit;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            // Check if we're looking at a destroyable object
            if (Physics.Raycast(ray, out hit, dragDistance, draggableLayer))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // If it's a destroyable object and player has attack tool, don't drag
                if (hitObject.CompareTag("Destroyable"))
                {
                    DestroyableObject destroyable = hitObject.GetComponent<DestroyableObject>();
                    if (destroyable != null)
                    {
                        return; // Let attack system handle it
                    }
                }
            }
        }
        
        // Raycast to see what we're looking at
        RaycastHit dragHit;
        Ray dragRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(dragRay, out dragHit, dragDistance, draggableLayer))
        {
            GameObject hitObject = dragHit.collider.gameObject;
            
            // Skip if it's the player
            if (hitObject.CompareTag("Player"))
            {
                return;
            }
            
            // Skip if it's a destroyable object and player has attack tool (already checked above, but double-check)
            if (hitObject.CompareTag("Destroyable") && IsPlayerHoldingAttackTool())
            {
                return;
            }
            
            // Check if object has a Rigidbody if required
            if (requireRigidbody)
            {
                Rigidbody rb = hitObject.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    // Try to get from parent
                    rb = hitObject.GetComponentInParent<Rigidbody>();
                }
                
                if (rb == null)
                {
                    return; // Can't drag without Rigidbody
                }
                
                draggedRigidbody = rb;
                draggedObject = rb.gameObject;
            }
            else
            {
                draggedObject = hitObject;
                draggedRigidbody = hitObject.GetComponent<Rigidbody>();
            }
            
            // Skip if object is being held in hotbar (check if it's inactive or parented to hand holder)
            if (IsObjectInHotbar(draggedObject))
            {
                Debug.Log($"[ObjectDragger] Cannot drag object '{draggedObject.name}' - it's in hotbar.");
                return;
            }
            
            // Skip if object is a placed item (has pickup component) - placed items should be picked up first
            if (IsPlacedItem(draggedObject))
            {
                Debug.Log($"[ObjectDragger] Cannot drag object '{draggedObject.name}' - it's a placed item. Pick it up (F) first, then drop it (Q) to make it draggable.");
                return;
            }
            
            StartDrag(dragHit);
        }
    }
    
    bool IsPlayerPlacingObject()
    {
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null) return false;
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return false;
        
        // Check if current item has a placement script (bed, campfire, etc.)
        if (currentItem.GetComponent<BedPlacement>() != null || 
            currentItem.GetComponent<CampfirePlacement>() != null)
        {
            return true;
        }
        
        return false;
    }
    
    bool IsPlayerHoldingAttackTool()
    {
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null) return false;
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return false;
        
        ItemIconProvider iconProvider = currentItem.GetComponent<ItemIconProvider>();
        if (iconProvider == null) return false;
        
        string itemName = iconProvider.itemName;
        if (string.IsNullOrEmpty(itemName)) return false;
        
        // Check if item name contains "axe", "rock", or "stone" (case-insensitive)
        string itemNameLower = itemName.ToLower();
        return itemNameLower.Contains("axe") || itemNameLower.Contains("rock") || itemNameLower.Contains("stone");
    }
    
    bool IsObjectInHotbar(GameObject obj)
    {
        // Check if object is inactive (likely in hotbar/inventory)
        if (!obj.activeInHierarchy)
        {
            return true;
        }
        
        // Check if object is parented to a hand holder (hotbar)
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.name.Contains("Hand") || parent.name.Contains("hand") || 
                parent.name.Contains("Holder") || parent.name.Contains("holder"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        // Check if object is on "Ignore Raycast" layer (usually means it's in inventory/hotbar)
        if (obj.layer == LayerMask.NameToLayer("Ignore Raycast"))
        {
            return true;
        }
        
        return false;
    }
    
    bool IsPlacedItem(GameObject obj)
    {
        // Check if object has a pickup component (CampfirePickup, BedPickup, WorkbenchPickup, etc.)
        // Placed items have pickup components, dropped items don't
        if (obj.GetComponent<CampfirePickup>() != null)
        {
            return true;
        }
        
        if (obj.GetComponent<BedPickup>() != null)
        {
            return true;
        }
        
        // Exclude workbenches from dragging (treat as placed/non-draggable)
        if (obj.GetComponent<WorkbenchPickup>() != null)
        {
            return true;
        }
        
        // Exclude chests from dragging (treat as placed/non-draggable)
        if (obj.GetComponent<ChestInteraction>() != null)
        {
            return true;
        }
        
        // Also check parent objects (in case the pickup component is on a parent)
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<CampfirePickup>() != null || 
                parent.GetComponent<BedPickup>() != null ||
                parent.GetComponent<WorkbenchPickup>() != null ||
                parent.GetComponent<ChestInteraction>() != null)
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    void StartDrag(RaycastHit hit)
    {
        isDragging = true;
        
        // Store original position and rotation
        originalPosition = draggedObject.transform.position;
        originalRotation = draggedObject.transform.rotation;
        lastValidPosition = originalPosition;
        
        // Calculate distance from camera to hit point
        float hitDistance = Vector3.Distance(playerCamera.transform.position, hit.point);
        dragDistanceFromCamera = Mathf.Clamp(hitDistance, minDragDistance, maxDragDistance);
        
        // Calculate offset from hit point to object center (for maintaining relative position)
        dragOffset = draggedObject.transform.position - hit.point;
        
        // Cache object bounds and colliders for collision detection
        CalculateObjectBounds();
        
        // Store and modify physics state
        if (draggedRigidbody != null)
        {
            wasKinematic = draggedRigidbody.isKinematic;
            usedGravity = draggedRigidbody.useGravity;
            originalDrag = draggedRigidbody.linearDamping;
            originalAngularDrag = draggedRigidbody.angularDamping;
            originalCollisionDetectionMode = draggedRigidbody.collisionDetectionMode;
            
            // Make kinematic and disable gravity for smooth dragging
            // BUT: Keep collision detection enabled so it can detect collisions
            draggedRigidbody.isKinematic = true;
            draggedRigidbody.useGravity = false;
            draggedRigidbody.linearDamping = 10f; // High drag for smooth movement
            draggedRigidbody.angularDamping = 10f;
            
            // Enable continuous collision detection for better collision detection
            draggedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        Debug.Log($"[ObjectDragger] Started dragging '{draggedObject.name}' at distance {dragDistanceFromCamera}");
    }
    
    void CalculateObjectBounds()
    {
        // Get all colliders on the object
        objectColliders = draggedObject.GetComponentsInChildren<Collider>();
        
        if (objectColliders != null && objectColliders.Length > 0)
        {
            // Calculate combined bounds
            objectBounds = objectColliders[0].bounds;
            for (int i = 1; i < objectColliders.Length; i++)
            {
                if (objectColliders[i] != null && objectColliders[i].enabled)
                {
                    objectBounds.Encapsulate(objectColliders[i].bounds);
                }
            }
        }
        else
        {
            // Fallback to simple sphere bounds
            Renderer renderer = draggedObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                objectBounds = renderer.bounds;
            }
            else
            {
                // Default bounds if no collider or renderer found
                objectBounds = new Bounds(draggedObject.transform.position, Vector3.one * collisionCheckRadius * 2f);
            }
        }
    }
    
    void UpdateDrag()
    {
        if (draggedObject == null)
        {
            StopDrag();
            return;
        }
        
        Vector3 currentPosition = draggedObject.transform.position;
        
        // Calculate target position based on camera ray - use center of screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Update drag distance dynamically based on where cursor is pointing
        // This allows the object to follow the cursor more naturally
        RaycastHit rayHit;
        if (Physics.Raycast(ray, out rayHit, maxDragDistance, obstacleLayer))
        {
            // If we hit something, use that distance (but maintain minimum distance)
            float hitDistance = Vector3.Distance(playerCamera.transform.position, rayHit.point);
            dragDistanceFromCamera = Mathf.Clamp(hitDistance - 0.5f, minDragDistance, maxDragDistance);
        }
        else
        {
            // If we don't hit anything, maintain the current distance or move it closer for responsiveness
            float currentDistance = Vector3.Distance(playerCamera.transform.position, currentPosition);
            dragDistanceFromCamera = Mathf.Clamp(currentDistance, minDragDistance, maxDragDistance);
        }
        
        // Calculate target position - object follows the ray at the calculated distance
        Vector3 targetPosition = ray.GetPoint(dragDistanceFromCamera) + dragOffset;
        
        // Clamp distance from player
        float distanceFromPlayer = Vector3.Distance(playerCamera.transform.position, targetPosition);
        if (distanceFromPlayer > maxDragDistance)
        {
            Vector3 direction = (targetPosition - playerCamera.transform.position).normalized;
            targetPosition = playerCamera.transform.position + direction * maxDragDistance;
        }
        
        // Check for collisions if enabled
        if (preventDragThroughWalls)
        {
            targetPosition = CheckCollisionsAndClampPosition(targetPosition, currentPosition);
        }
        
        // Only move if target position is valid (no collision)
        if (!HasCollisionAtPosition(targetPosition))
        {
            Vector3 newPosition;
            
            if (useInstantDrag)
            {
                // Instant movement - object follows cursor immediately
                newPosition = targetPosition;
            }
            else
            {
                // Smooth movement but much faster for better responsiveness
                // Use MoveTowards or very fast lerp for immediate feel
                float maxMoveDistance = dragSpeed * Time.deltaTime;
                newPosition = Vector3.MoveTowards(currentPosition, targetPosition, maxMoveDistance);
            }
            
            // Final collision check before moving
            if (!HasCollisionAtPosition(newPosition))
            {
                draggedObject.transform.position = newPosition;
                lastValidPosition = newPosition;
            }
            else
            {
                // Can't move there, stay at last valid position
                draggedObject.transform.position = lastValidPosition;
            }
        }
        else
        {
            // Target position has collision, stay at current position
            draggedObject.transform.position = lastValidPosition;
        }
    }
    
    Vector3 CheckCollisionsAndClampPosition(Vector3 targetPosition, Vector3 currentPosition)
    {
        // Calculate the movement vector
        Vector3 movement = targetPosition - currentPosition;
        
        if (movement.magnitude < 0.01f)
        {
            return targetPosition; // No significant movement
        }
        
        // Use continuous collision detection - check along the movement path
        float movementDistance = movement.magnitude;
        Vector3 movementDirection = movement.normalized;
        
        RaycastHit hit;
        
        // Calculate the size of the object for collision checking
        float checkRadius = Mathf.Max(objectBounds.extents.x, objectBounds.extents.z, objectBounds.extents.y);
        
        // Use the actual colliders if available for more accurate detection
        if (objectColliders != null && objectColliders.Length > 0)
        {
            // Check each collider individually
            foreach (Collider col in objectColliders)
            {
                if (col == null || !col.enabled) continue;
                
                // Calculate the collider's size and offset
                Vector3 colliderOffset = col.bounds.center - draggedObject.transform.position;
                Vector3 castOrigin = currentPosition + colliderOffset;
                
                // Get the collider's size
                float colRadius = 0f;
                float colHeight = 0f;
                
                if (col is SphereCollider)
                {
                    SphereCollider sphereCol = col as SphereCollider;
                    colRadius = sphereCol.radius * Mathf.Max(
                        draggedObject.transform.lossyScale.x,
                        draggedObject.transform.lossyScale.y,
                        draggedObject.transform.lossyScale.z
                    );
                    colHeight = colRadius * 2f;
                }
                else if (col is CapsuleCollider)
                {
                    CapsuleCollider capsuleCol = col as CapsuleCollider;
                    colRadius = capsuleCol.radius * Mathf.Max(
                        draggedObject.transform.lossyScale.x,
                        draggedObject.transform.lossyScale.z
                    );
                    colHeight = capsuleCol.height * draggedObject.transform.lossyScale.y;
                }
                else if (col is BoxCollider)
                {
                    BoxCollider boxCol = col as BoxCollider;
                    Vector3 size = Vector3.Scale(boxCol.size, draggedObject.transform.lossyScale);
                    colRadius = Mathf.Max(size.x, size.z) * 0.5f;
                    colHeight = size.y;
                }
                else
                {
                    // For other collider types, use bounds
                    colRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
                    colHeight = col.bounds.extents.y * 2f;
                }
                
                // Use CapsuleCast for better accuracy (works for most shapes)
                if (colHeight > colRadius * 2f)
                {
                    // Use capsule cast for tall objects
                    Vector3 point1 = castOrigin + Vector3.up * (colHeight * 0.5f - colRadius);
                    Vector3 point2 = castOrigin - Vector3.up * (colHeight * 0.5f - colRadius);
                    
                    if (Physics.CapsuleCast(
                        point1, point2, colRadius,
                        movementDirection,
                        out hit,
                        movementDistance,
                        obstacleLayer,
                        QueryTriggerInteraction.Ignore))
                    {
                        // Exclude the dragged object itself
                        if (hit.collider != null && hit.collider.gameObject != draggedObject &&
                            !hit.collider.transform.IsChildOf(draggedObject.transform) &&
                            !hit.collider.isTrigger)
                        {
                            // Stop just before the collision
                            float safeDistance = Mathf.Max(0f, hit.distance - collisionBuffer);
                            targetPosition = currentPosition + movementDirection * safeDistance;
                            return targetPosition;
                        }
                    }
                }
                else
                {
                    // Use sphere cast for round/short objects
                    if (Physics.SphereCast(
                        castOrigin,
                        colRadius,
                        movementDirection,
                        out hit,
                        movementDistance,
                        obstacleLayer,
                        QueryTriggerInteraction.Ignore))
                    {
                        // Exclude the dragged object itself
                        if (hit.collider != null && hit.collider.gameObject != draggedObject &&
                            !hit.collider.transform.IsChildOf(draggedObject.transform) &&
                            !hit.collider.isTrigger)
                        {
                            // Stop just before the collision
                            float safeDistance = Mathf.Max(0f, hit.distance - collisionBuffer);
                            targetPosition = currentPosition + movementDirection * safeDistance;
                            return targetPosition;
                        }
                    }
                }
                
                // Also check for overlaps at the target position
                Vector3 targetColliderCenter = targetPosition + colliderOffset;
                Collider[] overlaps = Physics.OverlapSphere(
                    targetColliderCenter,
                    colRadius + collisionBuffer,
                    obstacleLayer,
                    QueryTriggerInteraction.Ignore
                );
                
                foreach (Collider overlap in overlaps)
                {
                    if (overlap != null && overlap.gameObject != draggedObject &&
                        !overlap.transform.IsChildOf(draggedObject.transform) &&
                        !overlap.isTrigger)
                    {
                        // Collision at target position - use last valid position
                        return lastValidPosition;
                    }
                }
            }
        }
        else
        {
            // Fallback: use simple sphere cast with bounds
            if (Physics.SphereCast(
                currentPosition,
                checkRadius,
                movementDirection,
                out hit,
                movementDistance,
                obstacleLayer,
                QueryTriggerInteraction.Ignore))
            {
                // Exclude the dragged object itself
                if (hit.collider != null && hit.collider.gameObject != draggedObject &&
                    !hit.collider.transform.IsChildOf(draggedObject.transform) &&
                    !hit.collider.isTrigger)
                {
                    // Stop just before the collision
                    float safeDistance = Mathf.Max(0f, hit.distance - collisionBuffer);
                    targetPosition = currentPosition + movementDirection * safeDistance;
                    return targetPosition;
                }
            }
            
            // Check for overlaps at target position
            Collider[] targetOverlaps = Physics.OverlapSphere(
                targetPosition,
                checkRadius + collisionBuffer,
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );
            
            foreach (Collider overlap in targetOverlaps)
            {
                if (overlap != null && overlap.gameObject != draggedObject &&
                    !overlap.transform.IsChildOf(draggedObject.transform) &&
                    !overlap.isTrigger)
                {
                    // Collision at target position
                    return lastValidPosition;
                }
            }
        }
        
        return targetPosition;
    }
    
    bool HasCollisionAtPosition(Vector3 position)
    {
        // Check for overlaps at the target position using actual colliders
        if (objectColliders != null && objectColliders.Length > 0)
        {
            foreach (Collider col in objectColliders)
            {
                if (col == null || !col.enabled) continue;
                
                // Calculate the collider's position at target
                Vector3 colliderOffset = col.bounds.center - draggedObject.transform.position;
                Vector3 colliderPosition = position + colliderOffset;
                
                // Get collider size
                float checkRadius = 0f;
                if (col is SphereCollider)
                {
                    SphereCollider sphereCol = col as SphereCollider;
                    checkRadius = sphereCol.radius * Mathf.Max(
                        draggedObject.transform.lossyScale.x,
                        draggedObject.transform.lossyScale.y,
                        draggedObject.transform.lossyScale.z
                    );
                }
                else if (col is CapsuleCollider)
                {
                    CapsuleCollider capsuleCol = col as CapsuleCollider;
                    checkRadius = capsuleCol.radius * Mathf.Max(
                        draggedObject.transform.lossyScale.x,
                        draggedObject.transform.lossyScale.z
                    );
                }
                else if (col is BoxCollider)
                {
                    BoxCollider boxCol = col as BoxCollider;
                    Vector3 size = Vector3.Scale(boxCol.size, draggedObject.transform.lossyScale);
                    checkRadius = Mathf.Max(size.x, size.y, size.z) * 0.5f;
                }
                else
                {
                    checkRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
                }
                
                // Check for overlaps using OverlapSphere
                Collider[] overlaps = Physics.OverlapSphere(
                    colliderPosition,
                    checkRadius + collisionBuffer,
                    obstacleLayer,
                    QueryTriggerInteraction.Ignore
                );
                
                foreach (Collider overlap in overlaps)
                {
                    if (overlap != null && overlap.gameObject != draggedObject &&
                        !overlap.transform.IsChildOf(draggedObject.transform) &&
                        !overlap.isTrigger)
                    {
                        // Use Physics.ComputePenetration for accurate collision detection
                        // Calculate where the collider would be at the target position
                        Vector3 targetColPosition = colliderPosition;
                        Quaternion targetColRotation = col.transform.rotation;
                        
                        Vector3 direction;
                        float distance;
                        
                        // Check if the object's collider is penetrating the overlap collider at target position
                        if (Physics.ComputePenetration(
                            col, targetColPosition, targetColRotation,
                            overlap, overlap.transform.position, overlap.transform.rotation,
                            out direction, out distance))
                        {
                            // Penetration detected - this is a collision
                            return true;
                        }
                        
                        // Also check if it's a solid object (has Rigidbody, is static, or is terrain)
                        Rigidbody rb = overlap.GetComponent<Rigidbody>();
                        TerrainCollider terrainCollider = overlap as TerrainCollider;
                        
                        if (terrainCollider != null || overlap.gameObject.isStatic ||
                            (rb == null) || (rb != null && !rb.isKinematic))
                        {
                            return true; // Collision detected
                        }
                    }
                }
            }
        }
        else
        {
            // Fallback: use simple sphere check
            float checkRadius = Mathf.Max(objectBounds.extents.x, objectBounds.extents.y, objectBounds.extents.z, collisionCheckRadius);
            
            Collider[] overlaps = Physics.OverlapSphere(
                position, 
                checkRadius + collisionBuffer, 
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );
            
            foreach (Collider overlap in overlaps)
            {
                if (overlap != null && overlap.gameObject != draggedObject &&
                    !overlap.transform.IsChildOf(draggedObject.transform) &&
                    !overlap.isTrigger)
                {
                    // Check if it's a solid object (has Rigidbody, is static, or is terrain)
                    Rigidbody rb = overlap.GetComponent<Rigidbody>();
                    TerrainCollider terrainCollider = overlap as TerrainCollider;
                    
                    // Terrain and static objects always block
                    if (terrainCollider != null || overlap.gameObject.isStatic)
                    {
                        return true; // Collision detected
                    }
                    
                    // Non-kinematic rigidbodies block
                    if (rb != null && !rb.isKinematic)
                    {
                        return true; // Collision detected
                    }
                    
                    // Objects without rigidbody (colliders only) block
                    if (rb == null)
                    {
                        return true; // Collision detected
                    }
                }
            }
        }
        
        return false;
    }
    
    void StopDrag()
    {
        if (!isDragging || draggedObject == null)
        {
            return;
        }
        
        // Restore physics state
        if (draggedRigidbody != null)
        {
            draggedRigidbody.isKinematic = wasKinematic;
            draggedRigidbody.useGravity = usedGravity;
            draggedRigidbody.linearDamping = originalDrag;
            draggedRigidbody.angularDamping = originalAngularDrag;
            draggedRigidbody.collisionDetectionMode = originalCollisionDetectionMode;
        }
        
        Debug.Log($"[ObjectDragger] Stopped dragging '{draggedObject.name}'");
        
        // Clear references
        draggedObject = null;
        draggedRigidbody = null;
        isDragging = false;
    }
    
    void OnDisable()
    {
        // Stop dragging if component is disabled
        if (isDragging)
        {
            StopDrag();
        }
    }
    
    void OnDestroy()
    {
        // Stop dragging if component is destroyed
        if (isDragging)
        {
            StopDrag();
        }
    }
    
    // Visualize drag distance in editor
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerCamera.transform.position, dragDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerCamera.transform.position, maxDragDistance);
        }
    }
}

