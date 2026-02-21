using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Movement; // Access EnemyController

public class EnemyHealthManager : MonoBehaviour
{
    [Header("UI Setup")]
    public UIDocument uiDocument;
    public VisualTreeAsset healthBarTemplate;

    [Header("Settings")]
    public float verticalOffset = 2.0f;
    public Vector2 sizeOffset = new Vector2(-50, 0); // Centers a 100px bar

    // Dictionary is faster than List for lookups when removing
    private Dictionary<EnemyController, VisualElement> activeBars = new Dictionary<EnemyController, VisualElement>();
    
    private VisualElement root;
    private Camera mainCamera;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        mainCamera = Camera.main;

        // Subscribe to the STATIC events
        // This catches every enemy, no matter where or when they spawn
        EnemyController.OnEnemySpawned += RegisterEnemy;
        EnemyController.OnEnemyDespawned += UnregisterEnemy;
    }

    void OnDisable()
    {
        // Clean up static events to prevent memory leaks!
        EnemyController.OnEnemySpawned -= RegisterEnemy;
        EnemyController.OnEnemyDespawned -= UnregisterEnemy;
    }

    private void RegisterEnemy(EnemyController enemy)
    {
        if (activeBars.ContainsKey(enemy)) return; // Safety check

        // 1. Create UI
        VisualElement container = healthBarTemplate.Instantiate();
        VisualElement barRoot = container.Q("BarContainer");
        VisualElement barFill = container.Q("BarFill");
        
        root.Add(barRoot);
        barRoot.style.visibility = Visibility.Hidden; // Hide until positioned

        // 2. Initial Setup
        // Set to 100% immediately
        barFill.style.width = Length.Percent(100);

        // Store the listener so we can unsubscription later (optional, but good practice)
        enemy.OnHealthChanged += HealthListener;

        // 4. Store in Dictionary
        // We store the 'barRoot' because that's what we move/remove
        activeBars.Add(enemy, barRoot);
        return;

        // 3. Subscribe to THIS specific enemy's health change
        // We use a lambda to pass the specific barFill to the function
        void HealthListener(float pct)
        {
            barFill.style.width = Length.Percent(pct * 100);
        }
    }

    private void UnregisterEnemy(EnemyController enemy)
    {
        if (activeBars.TryGetValue(enemy, out VisualElement barRoot))
        {
            // Remove from UI
            if(root.Contains(barRoot)) root.Remove(barRoot);
            
            // Remove from Dictionary
            activeBars.Remove(enemy);
        }
    }

    void LateUpdate()
    {
        // Iterate through all active enemies in the dictionary
        foreach (var (enemy, bar) in activeBars)
        {
            if (!enemy) continue; // Should be handled by OnDisable, but safety first

            // 1. World Position with Offset
            Vector3 worldPos = enemy.transform.position + Vector3.up * verticalOffset;

            // 2. Check if in front of camera
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1)
            {
                bar.style.display = DisplayStyle.None;
                continue;
            }
            bar.style.display = DisplayStyle.Flex;
            bar.style.visibility = Visibility.Visible;

            // 3. Convert World -> Panel Space (The Fix from before)
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(bar.panel, worldPos, mainCamera);

            // 4. Apply Position
            bar.style.left = panelPos.x + sizeOffset.x;
            bar.style.top = panelPos.y + sizeOffset.y;
        }
    }
}