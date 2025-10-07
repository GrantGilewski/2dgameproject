using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace SoulfulJourney.Tests;

public class Test1
{
    private static readonly List<string> _testResults = new();
    private static int _testCount = 0;
    private static int _passedCount = 0;

    public static void Main(string[] args)
    {
        var test = new Test1();
        Console.WriteLine("Running tests with coverage tracking...");
        Console.WriteLine(new string('=', 50));
        
        try
        {
            // Basic tests
            test.ProgramFileExists();
            test.CsprojTargetsNet9OrSimilar();
            test.ProgramDefinesPlayerConstants();
            
            // Game logic tests
            test.GameEngineClassExists();
            test.MonoGameFrameworkUsed();
            test.GraphicsAndContentManagerInitialized();
            test.PlayerMovementLogicExists();
            test.JumpPhysicsImplemented();
            test.PlatformCollisionLogicExists();
            test.InputHandlingImplemented();
            test.RenderingLoopExists();
            test.TextureCreationLogicExists();
            test.GameLoopStructureValid();
            
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine($"Test Results: {_passedCount}/{_testCount} tests passed");
            
            // Simple coverage calculation
            var coveragePercent = CalculateSimpleCoverage();
            Console.WriteLine($"Estimated Coverage: {coveragePercent:F1}%");
            
            Console.WriteLine("\nAll tests passed! ✅");
            
            // Show examples of functional testing possibilities
            Console.WriteLine("\n" + new string('-', 50));
            Console.WriteLine("FUNCTIONAL TESTING EXAMPLES:");
            Console.WriteLine(new string('-', 50));
            var examples = new FunctionalTestExamples();
            examples.TestPlayerMovementCalculation();
            examples.TestPlayerPlatformCollision();
            examples.TestPlayerScreenBoundaries();
            examples.TestInputHandling();
            examples.TestRenderingSystem();
            
            var refactoring = new RefactoringNeeded();
            refactoring.ShowWhatWeNeed();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"Final Results: {_passedCount}/{_testCount} tests passed");
            Environment.Exit(1);
        }
    }

    private static void RecordTest(string testName, bool passed)
    {
        _testCount++;
        if (passed) _passedCount++;
        var status = passed ? "✅ PASS" : "❌ FAIL";
        Console.WriteLine($"{status} {testName}");
        _testResults.Add($"{testName}: {(passed ? "PASS" : "FAIL")}");
    }

    private static double CalculateSimpleCoverage()
    {
        // Realistic coverage assessment - what we actually test vs. what we should test
        var coverageItems = new Dictionary<string, bool>
        {
            // ✅ TESTED - Basic structural verification (smoke tests)
            ["Program.cs file exists"] = true,
            ["MyApp.csproj exists"] = true,  
            ["TargetFramework configured"] = true,
            ["Player constants defined"] = true,
            ["GameEngine class structure"] = true,
            ["MonoGame framework integration"] = true,
            ["Game loop methods exist"] = true,
            
            // 🟡 PARTIALLY TESTED - We verify existence, but not functionality
            ["Graphics/Content initialization works"] = false, // We don't test actual initialization
            ["Player movement calculations"] = false,          // We don't test movement math
            ["Jump physics accuracy"] = false,                 // We don't test gravity/velocity
            ["Platform collision precision"] = false,          // We don't test collision boundaries
            ["Input handling edge cases"] = false,             // We don't test simultaneous keys
            ["Rendering performance"] = false,                 // We don't test FPS/memory
            ["Texture loading error handling"] = false,        // We don't test missing files
            
            // ❌ NOT TESTED - Critical areas we should test
            ["Player boundary checking"] = false,              // Screen edges, out of bounds
            ["Platform collision edge cases"] = false,         // Corner cases, multiple platforms  
            ["Jump state transitions"] = false,                // Grounded->air->grounded
            ["Input state management"] = false,                // Key press vs hold detection
            ["Game initialization failure"] = false,           // What if graphics fail?
            ["Memory management"] = false,                     // Texture disposal, leaks
            ["Frame rate consistency"] = false,                // Performance under load
            ["Error recovery"] = false,                        // Game continues after errors
            ["Configuration validation"] = false,              // Invalid screen resolution
            ["Asset loading robustness"] = false               // Missing/corrupted files
        };
        
        var covered = coverageItems.Values.Count(x => x);
        var total = coverageItems.Count;
        var percentage = (double)covered / total * 100;
        
        Console.WriteLine($"\\nCoverage Breakdown:");
        Console.WriteLine($"  ✅ Structural Tests: 7/{total} items");
        Console.WriteLine($"  🟡 Functional Tests: 0/{total} items"); 
        Console.WriteLine($"  ❌ Edge Case Tests: 0/{total} items");
        Console.WriteLine($"  📊 Realistic Assessment: {percentage:F1}% (not 100%!)");
        
        return percentage;
    }

    // Helper: try a few likely relative paths from the test working directory to find the repo file
    private string? FindFile(params string[] relativePaths)
    {
        // Try each candidate relative path as-is and up to two parent directory shifts (bin/Debug/... runtime)
        foreach (var rel in relativePaths)
        {
            string candidate = Path.GetFullPath(rel);
            if (File.Exists(candidate)) return candidate;

            // also try going up one and two directories (test run dirs vary)
            var up1 = Path.GetFullPath(Path.Combine("..", rel));
            if (File.Exists(up1)) return up1;
            var up2 = Path.GetFullPath(Path.Combine("..", "..", rel));
            if (File.Exists(up2)) return up2;
        }
        return null;
    }

    public void ProgramFileExists()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        var passed = (path != null);
        RecordTest("ProgramFileExists", passed);
        if (!passed) throw new Exception("Could not find Program.cs in expected locations");
    }

    public void CsprojTargetsNet9OrSimilar()
    {
        var csproj = FindFile("../SoulfulJourney/MyApp.csproj", "SoulfulJourney/MyApp.csproj", "../MyApp.csproj");
        var passed = (csproj != null);
        if (!passed) 
        {
            RecordTest("CsprojTargetsNet9OrSimilar", false);
            throw new Exception("Could not find MyApp.csproj");
        }

        var content = File.ReadAllText(csproj!);
        // Accept net9.0 or other modern TFMs; ensure a TargetFramework element exists
        passed = Regex.IsMatch(content, @"<TargetFramework>\s*net[0-9]\.[0-9]\s*</TargetFramework>", RegexOptions.IgnoreCase);
        RecordTest("CsprojTargetsNet9OrSimilar", passed);
        if (!passed) throw new Exception("MyApp.csproj does not contain expected TargetFramework");
    }

    public void ProgramDefinesPlayerConstants()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null) 
        {
            RecordTest("ProgramDefinesPlayerConstants", false);
            throw new Exception("Could not find Program.cs to inspect Player constants");
        }

        var src = File.ReadAllText(path!);
        var mW = Regex.Match(src, @"PlayerWidth\s*=\s*(\d+)");
        var mH = Regex.Match(src, @"PlayerHeight\s*=\s*(\d+)");
        var passed = mW.Success && mH.Success && mW.Groups[1].Value == "32" && mH.Groups[1].Value == "32";
        RecordTest("ProgramDefinesPlayerConstants", passed);
        
        if (!mW.Success || !mH.Success) throw new Exception("PlayerWidth/PlayerHeight constants not found");
        if (mW.Groups[1].Value != "32") throw new Exception($"PlayerWidth expected 32, got {mW.Groups[1].Value}");
        if (mH.Groups[1].Value != "32") throw new Exception($"PlayerHeight expected 32, got {mH.Groups[1].Value}");
    }

    public void GameEngineClassExists()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("GameEngineClassExists", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasGameClass = Regex.IsMatch(src, @"class\s+GameEngine\s*:\s*Game", RegexOptions.IgnoreCase);
        RecordTest("GameEngineClassExists", hasGameClass);
        if (!hasGameClass) throw new Exception("GameEngine class inheriting from Game not found");
    }

    public void MonoGameFrameworkUsed()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("MonoGameFrameworkUsed", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasMonoGame = src.Contains("Microsoft.Xna.Framework") || 
                         src.Contains("GraphicsDeviceManager") || 
                         src.Contains("SpriteBatch");
        RecordTest("MonoGameFrameworkUsed", hasMonoGame);
        if (!hasMonoGame) throw new Exception("MonoGame framework usage not detected");
    }

    public void GraphicsAndContentManagerInitialized()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("GraphicsAndContentManagerInitialized", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasGraphics = src.Contains("GraphicsDeviceManager") && src.Contains("_graphics");
        var hasContent = src.Contains("Content.RootDirectory");
        var passed = hasGraphics && hasContent;
        RecordTest("GraphicsAndContentManagerInitialized", passed);
        if (!passed) throw new Exception("Graphics/Content manager initialization not found");
    }

    public void PlayerMovementLogicExists()
    {
        var programPath = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        var playerPath = FindFile("../SoulfulJourney/ProjectFiles/ObjectCode/Player.cs", "SoulfulJourney/ProjectFiles/ObjectCode/Player.cs", "../ProjectFiles/ObjectCode/Player.cs");
        
        if (programPath == null)
        {
            RecordTest("PlayerMovementLogicExists", false);
            throw new Exception("Could not find Program.cs");
        }

        var programSrc = File.ReadAllText(programPath);
        var hasPlayerControlsCall = programSrc.Contains("player.controls()") || programSrc.Contains(".controls()");
        
        bool hasMovementKeys = false;
        if (playerPath != null)
        {
            var playerSrc = File.ReadAllText(playerPath);
            hasMovementKeys = (playerSrc.Contains("Keys.Left") || playerSrc.Contains("Keys.Right") ||
                              playerSrc.Contains("Keys.A") || playerSrc.Contains("Keys.D")) &&
                              (playerSrc.Contains("xspeed") || playerSrc.Contains("playerSpeed"));
        }
        
        var hasMovement = hasPlayerControlsCall || hasMovementKeys ||
                         programSrc.Contains("Keys.Left") || programSrc.Contains("Keys.Right") ||
                         programSrc.Contains("Keys.A") || programSrc.Contains("Keys.D");
        
        RecordTest("PlayerMovementLogicExists", hasMovement);
        if (!hasMovement) throw new Exception("Player movement logic not detected in Program.cs or Player.cs");
    }

    public void JumpPhysicsImplemented()
    {
        var programPath = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        var playerPath = FindFile("../SoulfulJourney/ProjectFiles/ObjectCode/Player.cs", "SoulfulJourney/ProjectFiles/ObjectCode/Player.cs", "../ProjectFiles/ObjectCode/Player.cs");
        
        if (programPath == null)
        {
            RecordTest("JumpPhysicsImplemented", false);
            throw new Exception("Could not find Program.cs");
        }

        var programSrc = File.ReadAllText(programPath);
        bool hasJump = false;
        
        // Check Program.cs for jump logic
        hasJump = programSrc.Contains("_jumpVelocity") || programSrc.Contains("_isJumping") || 
                  programSrc.Contains("_gravity") || programSrc.Contains("Keys.Space") ||
                  Regex.IsMatch(programSrc, @"jump|Jump", RegexOptions.IgnoreCase);
        
        // Also check Player.cs if it exists
        if (!hasJump && playerPath != null)
        {
            var playerSrc = File.ReadAllText(playerPath);
            hasJump = playerSrc.Contains("jumpStrength") || playerSrc.Contains("inAir") ||
                     playerSrc.Contains("yspeed") || playerSrc.Contains("Keys.Space") ||
                     playerSrc.Contains("Keys.W") || playerSrc.Contains("Keys.Up") ||
                     Regex.IsMatch(playerSrc, @"jump|Jump", RegexOptions.IgnoreCase);
        }
        
        RecordTest("JumpPhysicsImplemented", hasJump);
        if (!hasJump) throw new Exception("Jump physics implementation not detected in Program.cs or Player.cs");
    }

    public void PlatformCollisionLogicExists()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("PlatformCollisionLogicExists", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasPlatforms = src.Contains("_platforms") || src.Contains("Platform") ||
                          Regex.IsMatch(src, @"collision|Collision", RegexOptions.IgnoreCase) ||
                          src.Contains("Rectangle.Intersects");
        RecordTest("PlatformCollisionLogicExists", hasPlatforms);
        if (!hasPlatforms) throw new Exception("Platform collision logic not detected");
    }

    public void InputHandlingImplemented()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("InputHandlingImplemented", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasInput = src.Contains("Keyboard.GetState") || src.Contains("KeyboardState") ||
                       src.Contains("_previousKeyboardState") ||
                       Regex.IsMatch(src, @"Keys\.[A-Z]", RegexOptions.IgnoreCase);
        RecordTest("InputHandlingImplemented", hasInput);
        if (!hasInput) throw new Exception("Input handling logic not detected");
    }

    public void RenderingLoopExists()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("RenderingLoopExists", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasRendering = src.Contains("SpriteBatch") && src.Contains("Draw(") &&
                          (src.Contains("_spriteBatch.Begin") || src.Contains("spriteBatch.Begin"));
        RecordTest("RenderingLoopExists", hasRendering);
        if (!hasRendering) throw new Exception("Rendering loop logic not detected");
    }

    public void TextureCreationLogicExists()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("TextureCreationLogicExists", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasTextures = src.Contains("Texture2D") && 
                         (src.Contains("SetData") || src.Contains("LoadContent") || 
                          src.Contains("_playerTexture") || src.Contains("_platformTexture"));
        RecordTest("TextureCreationLogicExists", hasTextures);
        if (!hasTextures) throw new Exception("Texture creation logic not detected");
    }

    public void GameLoopStructureValid()
    {
        var path = FindFile("../SoulfulJourney/Program.cs", "SoulfulJourney/Program.cs", "../Program.cs");
        if (path == null)
        {
            RecordTest("GameLoopStructureValid", false);
            throw new Exception("Could not find Program.cs");
        }

        var src = File.ReadAllText(path);
        var hasUpdate = Regex.IsMatch(src, @"protected\s+override\s+void\s+Update", RegexOptions.IgnoreCase);
        var hasDraw = Regex.IsMatch(src, @"protected\s+override\s+void\s+Draw", RegexOptions.IgnoreCase);
        var hasLoadContent = Regex.IsMatch(src, @"protected\s+override\s+void\s+LoadContent", RegexOptions.IgnoreCase);
        var passed = hasUpdate && hasDraw && hasLoadContent;
        RecordTest("GameLoopStructureValid", passed);
        if (!passed) throw new Exception("Game loop structure (Update/Draw/LoadContent) not complete");
    }
}
