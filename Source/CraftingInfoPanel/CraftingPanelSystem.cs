using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace DuravoQOLMod.CraftingInfoPanel
{
    /// <summary>
    /// ModSystem that manages the Crafting Info Panel UI lifecycle.
    /// Handles UI visibility, updates, and rendering layer integration.
    /// </summary>
    public class CraftingPanelSystem : ModSystem
    {
        /// <summary>The UserInterface managing the panel state</summary>
        internal UserInterface craftingPanelInterface;
        
        /// <summary>The main panel UIState</summary>
        internal CraftingInfoPanelUI craftingPanelState;
        
        /// <summary>Whether the panel is currently visible</summary>
        private bool isPanelVisible = false;
        
        /// <summary>Singleton accessor for other classes to use</summary>
        public static CraftingPanelSystem Instance => ModContent.GetInstance<CraftingPanelSystem>();
        
        /// <summary>Public accessor for panel visibility</summary>
        public bool IsPanelVisible => isPanelVisible;
        
        public override void Load()
        {
            // UI is client-side only - don't create on dedicated server
            if (Main.dedServ) {
                return;
            }
            
            craftingPanelInterface = new UserInterface();
            craftingPanelState = new CraftingInfoPanelUI();
            craftingPanelState.Activate();
        }
        
        public override void Unload()
        {
            // Clean up references
            craftingPanelState = null;
            craftingPanelInterface = null;
        }
        
        /// <summary>
        /// Toggle panel visibility. Called by the toggle button.
        /// </summary>
        public void TogglePanel()
        {
            isPanelVisible = !isPanelVisible;
            
            if (isPanelVisible) {
                craftingPanelInterface?.SetState(craftingPanelState);
            } else {
                craftingPanelInterface?.SetState(null);
            }
        }
        
        /// <summary>
        /// Force panel to close. Called when inventory closes.
        /// </summary>
        public void ClosePanel()
        {
            isPanelVisible = false;
            craftingPanelInterface?.SetState(null);
        }
        
        /// <summary>
        /// Check if player is near any relevant crafting station.
        /// Uses Main.LocalPlayer.adjTile[] which tracks nearby tiles.
        /// </summary>
        public static bool IsNearCraftingStation()
        {
            Player localPlayer = Main.LocalPlayer;
            
            // adjTile is true when player is within crafting range of that tile type
            return localPlayer.adjTile[TileID.WorkBenches] ||
                   localPlayer.adjTile[TileID.Furnaces] ||
                   localPlayer.adjTile[TileID.Anvils] ||
                   localPlayer.adjTile[TileID.MythrilAnvil]; // Also check hardmode anvils
        }
        
        public override void UpdateUI(GameTime gameTime)
        {
            // Only update when visible and panel exists
            if (isPanelVisible && craftingPanelInterface?.CurrentState != null) {
                craftingPanelInterface.Update(gameTime);
            }
            
            // Auto-close when inventory closes
            if (isPanelVisible && !Main.playerInventory) {
                ClosePanel();
            }
        }
        
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Find the inventory layer to insert after it
            int inventoryLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            
            if (inventoryLayerIndex != -1) {
                // Insert our panel layer right after inventory
                layers.Insert(inventoryLayerIndex + 1, new LegacyGameInterfaceLayer(
                    "DuravoQOLMod: Crafting Info Panel",
                    delegate {
                        if (isPanelVisible && craftingPanelInterface?.CurrentState != null) {
                            craftingPanelInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
                
                // Also add the toggle button layer (always visible when inventory is open)
                layers.Insert(inventoryLayerIndex + 1, new LegacyGameInterfaceLayer(
                    "DuravoQOLMod: Crafting Panel Toggle Button",
                    delegate {
                        if (Main.playerInventory) {
                            CraftingPanelButton.Draw(Main.spriteBatch);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}