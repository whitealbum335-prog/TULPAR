 Tulpar

Tulpar is an educational rocket launch simulator with a 2.5D flight view. The browser version is a self-contained static page; a Windows desktop version is also included.

## Run the browser version

No installation or build step is required:

1. Download or clone this repository.
2. Open `index.html` in a modern web browser.
3. Configure the vehicle and select **Launch simulation**.

The simulator runs locally in the browser and does not need an internet connection, .NET, or an installer.

## Run the Windows desktop version

The WPF version requires Windows and the .NET 8 SDK. From the project folder, run:

```powershell
dotnet run --project Tulpar.csproj
```

To build a Release version:

```powershell
dotnet publish Tulpar.csproj --configuration Release
```

## Publish the browser version with GitHub Pages

The root `index.html` can be hosted directly as a static site:

1. Push this repository to GitHub.
2. Open the repository's **Settings** → **Pages**.
3. Under **Build and deployment**, choose **Deploy from a branch**.
4. Select the `main` branch and `/ (root)`, then save.
5. Once deployment finishes, GitHub Pages will show the site URL in the Pages settings.

The browser version does not require a build workflow. The WPF desktop app cannot run on GitHub Pages.

## Project files

- `index.html` — standalone browser simulator.
- `Tulpar.csproj`, `App.xaml`, `MainWindow.xaml`, and their C# code-behind files — Windows WPF desktop app.
- `FlightSimulation.cs` — desktop app's flight physics model.

## Current simulation

- Vertical launch with lateral crosswind drift to a 120 km near-space checkpoint
- Variable mass with propellant flow derived from thrust and specific impulse
- Layered standard-atmosphere pressure and temperature, with an exponential upper-atmosphere extension
- Spherical-Earth gravity, quadratic drag, and a simplified transonic drag rise
- Fourth-order Runge–Kutta integration of altitude and downrange motion
- Calm, windy, and thunderstorm weather profiles with altitude-varying wind, deterministic gusts, rain, and animated lightning
- A simulated in-flight failure when modeled lateral wind pressure exceeds the illustrative vehicle limit; reset restores calm weather
- Flight-time readout reports simulated vehicle time; animation plays at 8× so the ascent is quicker to watch
- Delayed hover explanations for rocket mass, propellant, thrust, burn time, drag, diameter, specific impulse, and weather
- Live altitude, total speed, acceleration, downrange distance, crosswind, Mach number, dynamic pressure, time, and altitude-profile readouts
- Animated wind-driven clouds, storm rain and lightning, exhaust, launch smoke, atmospheric transition, and receding Earth

The default calm-weather vehicle is an illustrative single-stage launch scenario. It reaches the 120 km near-space checkpoint in roughly 3:09 of simulated flight; the animation is sped up for viewing. The checkpoint is not orbit: the model does not build orbital horizontal velocity. The 2.5D view and weather failures are educational approximations, not validated flight predictions. In particular, the model uses a simplified side area and an illustrative lateral-pressure failure threshold; it does not simulate vehicle attitude dynamics, guidance, staging, detailed engine curves, or real launch commit criteria. The weather model is intentionally a small deterministic scenario set, not a forecast. [NASA Glenn's atmosphere overview](https://www.grc.nasa.gov/www/k-12/airplane/atmosmet.html) explains how standard-atmosphere properties vary with altitude and notes that actual atmospheric conditions vary with time and place. For a comparison with a more advanced educational rocket simulator, [OpenRocket's feature overview](https://openrocket.info/features.html) describes six-degree-of-freedom flight, realistic wind modeling, and component-based stability analysis.

## License

No license has been included. Without a license, normal copyright restrictions apply; add a license before inviting others to reuse or modify this project.
