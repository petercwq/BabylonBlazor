﻿namespace BabylonBlazor.Client.Game;

using System;
using System.Threading.Tasks;
using BABYLON;
using BABYLON.GUI;
using BabylonBlazor.Client.HTML;
using EventHorizon.Blazor.Interop.Callbacks;
using Scene = Extensions.DebugLayerScene;

public class GameApp : IDisposable
{
    // General Entire Application
    private readonly Canvas _canvas;
    private readonly Engine _engine;

    // Scene - related
    private Scene _scene;
    private GameState _state = GameState.Start;
    private Scene _gameScene;
    private Scene _cutScene;

    public DebugLayer DebugLayer => _scene.debugLayer;

    public GameApp(string canvasId)
    {
        _canvas = CreateCanvas(canvasId);

        // Initialize Babylon Scene and Engine
        _engine = new Engine(_canvas, antialias: true);
        _scene = new Scene(_engine);

        var camera = new ArcRotateCamera(
            name: "Camera",
            (decimal)(Math.PI / 2),
            (decimal)(Math.PI / 2),
            radius: 2,
            Vector3.Zero(),
            _scene
        );
        _scene.activeCamera = camera;
        camera.attachControl(true);
    }

    public async Task Main()
    {
        await GoToStart();

        _engine.runRenderLoop(new ActionCallback(
            () =>
            {
                switch (_state)
                {
                    case GameState.Start:
                        return Task.Run(() => _scene.render(true, false));
                    case GameState.Game:
                        return Task.Run(() => _scene.render(true, false));
                    case GameState.Lose:
                        return Task.Run(() => _scene.render(true, false));
                    case GameState.CutScene:
                        return Task.Run(() => _scene.render(true, false));
                    default:
                        break;
                }
                return Task.CompletedTask;
            }
        ));
    }

    public void Resize()
    {
        _engine.resize();
    }

    public void Dispose()
    {
        _engine.dispose();
    }

    private static Canvas CreateCanvas(string canvasId)
    {
        return Canvas.GetElementById(canvasId);
    }

    private Task SetupGame()
    {
        var scene = new Scene(_engine);
        _gameScene = scene;

        // Load Assets

        return Task.CompletedTask;
    }

    #region Go To Start
    public async Task GoToStart()
    {
        _engine.displayLoadingUI();

        _scene.detachControl();
        var scene = new Scene(_engine)
        {
            clearColor = new Color4(0, 0, 0, 1)
        };
        var camera = new FreeCamera("camera1", new Vector3(0, 0, 0), scene);
        scene.activeCamera = camera;
        camera.setTarget(Vector3.Zero());
        camera.attachControl(true);

        var guiMenu = AdvancedDynamicTexture.CreateFullscreenUI(
            name: "UI",
            scene: scene
        );
        guiMenu.idealHeight = 720;

        var startButton = Button.CreateSimpleButton(
            "start",
            "PLAY"
        );
        startButton.width = "0.2";
        startButton.height = "40px";
        startButton.color = "white";
        startButton.top = "-14px";
        startButton.thickness = 0;
        startButton.verticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
        guiMenu.addControl(startButton);

        startButton.onPointerDownObservable.add(async (_, __) =>
        {
            await GoToCutScene();
            scene.detachControl();
        });

        await scene.whenReadyAsync();
        _engine.hideLoadingUI();

        _scene.dispose();
        _scene = scene;
        _state = GameState.Start;
    }
    #endregion

    private async Task GoToCutScene()
    {
        _engine.displayLoadingUI();

        // Setup Scene
        _scene.detachControl();
        _cutScene = new Scene(_engine);
        var camera = new FreeCamera("camera1", position: new Vector3(0, 0, 0), _cutScene);
        _cutScene.activeCamera = camera;
        camera.setTarget(Vector3.Zero());
        camera.attachControl(true);

        // GUI
        var cutScene = AdvancedDynamicTexture.CreateFullscreenUI(
            name: "cutscene",
            scene: _cutScene
        );

        //--PROGRESS DIALOGUE--
        var next = Button.CreateSimpleButton(
            "next",
            "NEXT"
        );
        next.color = "white";
        next.thickness = 0;
        next.verticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
        next.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_RIGHT;
        next.width = "64px";
        next.height = "64px";
        next.top = "-3%";
        next.left = "-12%";
        cutScene.addControl(next);

        next.onPointerUpObservable.add(async (_, __) =>
        {
            await GoToGame();
        });

        // Scene Finished Loading
        await _cutScene.whenReadyAsync();
        _scene.dispose();
        _state = GameState.CutScene;
        _scene = _cutScene;

        _engine.hideLoadingUI();

        // Start Loading and Setup the Game
        await SetupGame();
    }

    private async Task GoToGame()
    {
        _scene.detachControl();
        var scene = _gameScene;
        scene.clearColor = new Color4(
            0.01568627450980392m,
            0.01568627450980392m,
            0.20392156862745098m,
            1
        ); // a color that fit the overall color scheme better
        var camera = new ArcRotateCamera(
            "Camera",
            (decimal)Math.PI / 2,
            (decimal)Math.PI / 2,
            radius: 2,
            Vector3.Zero(),
            scene
        );
        scene.activeCamera = camera;
        camera.setTarget(Vector3.Zero());
        camera.attachControl(true);

        //--GUI--
        var playerUI = AdvancedDynamicTexture.CreateFullscreenUI(
            name: "UI",
            scene: scene
        );
        playerUI.idealHeight = 720;
        //dont detect any inputs from this ui while the game is loading
        scene.detachControl();

        //create a simple button
        var loseBtn = Button.CreateSimpleButton(
            "lose",
            "LOSE"
        );
        loseBtn.width = "0.2";
        loseBtn.height = "40px";
        loseBtn.color = "white";
        loseBtn.top = "-14px";
        loseBtn.thickness = 0;
        loseBtn.verticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
        playerUI.addControl(loseBtn);

        //this handles interactions with the start button attached to the scene
        loseBtn.onPointerUpObservable.add(async (_, __) =>
        {
            await GoToLose();
            scene.detachControl(); //observables disabled
        });

        //temporary scene objects
        var light1 = new HemisphericLight(
            "light1",
            new Vector3(1, 1, 0),
            scene
        );
        var sphere = MeshBuilder.CreateSphere(
            "sphere",
            new
            {
                diameter = 1
            },
            scene
        );

        //get rid of start scene, switch to gamescene and change states
        _scene.dispose();
        _state = GameState.Game;
        _scene = scene;
        _engine.hideLoadingUI();

        //the game is ready, attach control back
        _scene.attachControl(
            true,
            true,
            true
        );
    }

    private async Task GoToLose()
    {
        _engine.displayLoadingUI();

        // Scene Setup
        _scene.detachControl();
        var scene = new Scene(_engine)
        {
            clearColor = new Color4(0, 0, 0, 1)
        };
        var camera = new FreeCamera("camera1", new Vector3(0, 0, 0), scene);
        scene.activeCamera = camera;
        camera.setTarget(Vector3.Zero());
        camera.attachControl(true);

        // GUI Setup
        var guiMenu = AdvancedDynamicTexture.CreateFullscreenUI(
            name: "UI",
            scene: scene
        );
        var mainButton = Button.CreateSimpleButton(
            "mainmenu",
            "MAIN MENU"
        );
        mainButton.width = "0.2";
        mainButton.height = "40px";
        mainButton.color = "white";
        guiMenu.addControl(mainButton);
        mainButton.onPointerUpObservable.add(async (_, __) =>
        {
            await GoToStart();
        });

        // Scene Finished Loading
        await scene.whenReadyAsync();
        _engine.hideLoadingUI();

        _scene.dispose();
        _scene = scene;
        _state = GameState.Lose;
    }
}