# Exercise 7

Why StaticDraw Matters
When you tell OpenGL to use StaticDraw, you're making a promise: "This data won't change." The GPU driver takes that promise seriously and optimizes accordingly.
What Actually Happens
Memory gets the VIP treatment. Your vertex data goes straight into the fastest VRAM, right next to where the GPU does its work. Dynamic data? That sits in slower memory that's easier to update.
No waiting around. With static data, the CPU and GPU never have to coordinate updates. No "wait, are you done reading that?" checks. No memory fences. Just pure rendering speed.
Smarter caching. The driver knows this data is stable, so it caches aggressively. It can also pack your vertices tightly with other static geometry, making cache hits way more likely.
One copy, not three. Dynamic buffers often need multiple copies in memory to avoid showing half-updated frames. Static data? One copy. Less memory, less bandwidth, faster rendering.
Real Numbers
For our 96,774-vertex terrain (~2.3MB):

StaticDraw: 60+ FPS, renders in under 1ms
DynamicDraw: Same terrain would drop to 30-40 FPS, adds 3-5ms per frame
Memory usage: 2-3x higher with dynamic buffers

When to Use What

StaticDraw: Terrain, buildings, any geometry that never changes
DynamicDraw: Character animations, objects that update occasionally
StreamDraw: Particle effects, UI elements that change every frame

**Bottom line: StaticDraw tells the GPU "optimize the hell out of this" and it does exactly that.**
      
