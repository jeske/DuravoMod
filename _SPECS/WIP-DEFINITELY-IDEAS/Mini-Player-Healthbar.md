
TODO: write a spec for a small character health/mana bar that appears just BELOW the character's feet when meeting certain conditions (this is akin to the healthbar that appears above the head in ARPGS like diablo and POE, but for terraria it will be better below the character).... but auto-hides when not necessary... goal is to make it easier to see dmg at a glance when you are in trouble, without having to look all the way at the upper right corner OR have a mod that puts a giant healthbar in the middlebottom of your screen.. the conditions for activating it might be "health < 50% or 4 hearts", recent incoming dmg > 25% of health.....

the health bar will have 3 sections 

- simple 1-2pixel border that represents 100% health
- "current available health" (green)
- "recently lost health" (yellow) an animated shrinking of the healthbar that helps communicating how big that recent hit was
- "shield" (blue) if an armor shield is active. if health is at 100% this should overflow "beyond" the 100% width of the healthbar (but drawn underneath the border
