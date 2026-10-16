**🌦️ AURA-WORLD**

🌍 Adaptive Real-World Telemetry Environment

🏙️ A Weather-Responsive 3D Smart City Simulation Built with Unity and C#

AURA-WORLD is a 3D smart city simulation project that explores how real-world weather data can influence a virtual environment. By connecting weather telemetry with Unity, the project aims to make environmental conditions visible through lighting, rain, wind, fog, and road appearance.

👥 Team: IoTrix
🎮 Domain: 3D Simulation · Weather Integration · Interactive Environments
🛠️ Platform: Unity
💻 Language: C#
🌐 Weather Provider: OpenWeatherMap API
📌 Project Type: Academic Prototype

---

📑 Table of Contents

- 🌟 "Overview" 
- ❗ "Problem Statement" 
- 🎯 "Objectives" 
- ✨ "Features" 
- 🧰 "Technology Stack" 
- 🏗️ "System Architecture" 
- 📂 "Project Structure" 
- 🌐 "Weather Data and Offline Support" 
- 🚀 "Getting Started" 
- ⚙️ "Configuration" 
- 🧪 "Testing and Validation" 
- 📊 "Current Status" 
- 🔮 "Future Enhancements" 
- 👥 "Team" 
- 📜 "License" 

---

🌟 Overview

Traditional virtual environments often remain static and require developers to trigger environmental changes manually.

AURA-WORLD explores a data-driven approach in which weather information can control the behaviour and appearance of a virtual city. The project combines a 3D urban scene, weather-related C# logic, a city-search interface, and environmental visual systems.

The intended workflow is to retrieve weather information, process the response, and use the relevant values to update the virtual environment.

The project also includes a design for cached weather data and a local JSON fallback, intended to help with testing when live weather requests are unavailable.

❗ Problem Statement

🌍 Real-world weather changes continuously, but many virtual environments do not reflect these changes automatically.

AURA-WORLD aims to explore how external weather data can be connected to a 3D simulation to create a more responsive and immersive environment.

🎯 Objectives

- 🌐 Integrate real-world weather information into Unity.
- 📡 Retrieve weather data through an external API.
- 🔄 Process JSON responses using C#.
- 🏙️ Create a 3D urban environment with roads, buildings, trees, and vehicles.
- 🌧️ Connect weather conditions to environmental effects.
- 💡 Explore weather-responsive lighting and day/night visuals.
- 🌬️ Incorporate wind and atmospheric effects where configured.
- 🧭 Provide a city-search interface for requesting weather information.
- 📴 Support offline testing through cached or sample weather data.
- 🛡️ Handle API errors and connectivity problems gracefully.

✨ Features

🏙️ 1. 3D Smart City Environment

The Unity project includes urban and environmental assets designed to create a virtual city.

- 🛣️ Roads and road materials
- 🏢 Buildings and architectural assets
- 🌳 Trees and vegetation
- 🚗 Vehicles
- 🎨 Materials and visual assets
- 🌆 Scene and environment configuration

🌦️ 2. Weather Data Integration

The project is designed to retrieve weather information using the OpenWeatherMap API.

The intended data workflow includes:

- 📡 Sending an HTTP request from Unity.
- 📥 Receiving a weather response.
- 🧩 Parsing JSON data using C#.
- ⚠️ Handling failed requests and unavailable data.
- 🔗 Passing weather values to environment-control logic.

Live weather retrieval requires internet connectivity and valid API configuration.

🌧️ 3. Weather-Responsive Environmental Effects

The project contains code intended to connect weather conditions to multiple visual systems.

🌤️ System| 🎯 Intended behaviour
💡 Lighting| Adjust environmental illumination
🌧️ Rain particles| Represent rainy weather
🌬️ Wind| Control wind-related scene behaviour
🌫️ Fog| Adjust atmospheric visibility
🛣️ Road materials| Reflect environmental conditions
🌗 Day/night visuals| Support different lighting states

Note: Each effect must be verified in the Unity scene before it is described as fully functional.

🖥️ 4. Weather Search Interface

The project includes Unity UI elements for entering a city and displaying weather-related information.

The interface should be validated to ensure that:

- 🔎 City input is received correctly.
- 📊 Actual API values appear in the results.
- ⚠️ Invalid cities and failed requests are handled clearly.
- 🔄 Displayed information updates after a successful request.

💾 5. Cached Weather Data and JSON Fallback

The project design includes cached responses and a local sample JSON fallback path.

These mechanisms are intended to help test the application without depending entirely on live API access.

The fallback requires the appropriate JSON asset and matching parser configuration.

---

🧰 Technology Stack

🛠️ Technology| 💡 Purpose
🎮 Unity| 3D simulation and scene rendering
💻 C#| Application logic and weather processing
🌐 OpenWeatherMap API| External weather information
📄 JSON| Weather-response and sample-data format
🖥️ Unity UI| City input and weather display
🌧️ Particle Systems| Environmental effects
🌬️ Wind Zone| Wind-related scene behaviour
🌿 3D Assets and Materials| City environment and visual presentation
🔀 Git| Version control
🐙 GitHub| Source-code hosting and collaboration

🏗️ System Architecture

The intended system workflow is:

👤 User enters a city
          |
          v
🖥️ Unity Weather Interface
          |
          v
💻 C# Weather Request Logic
          |
          v
🌐 OpenWeatherMap API
          |
          v
📄 JSON Response Processing
          |
          v
🌦️ Weather Values and Error Handling
          |
          v
🏙️ Environment-Control Logic
          |
          v
💡 Lighting | 🌧️ Rain | 🌬️ Wind
🌫️ Fog | 🛣️ Road Visuals

If a live request fails, cached or sample data can be used only when the corresponding fallback path is correctly configured.

📂 Project Structure

AURA-WORLD/
│
├── 📁 Assets/
│   ├── 🔊 Audio/
│   ├── 🛠️ Editor/
│   ├── 🏗️ Models/
│   ├── 📦 Resources/
│   ├── 🎬 Scenes/
│   ├── 💻 Scripts/
│   ├── ⚙️ Settings/
│   ├── 🔤 TextMesh Pro/
│   └── 📘 TutorialInfo/
│
├── 📁 Packages/
├── 📁 ProjectSettings/
├── 📄 .gitattributes
├── 📄 .gitignore
└── 📄 README.md

📁 Folder Guide

- 🎬 "Assets/Scenes/" — Unity scenes.
- 💻 "Assets/Scripts/" — C# source files.
- 🏗️ "Assets/Models/" — 3D models and related assets.
- 📦 "Assets/Resources/" — runtime-loadable resources.
- 🔊 "Assets/Audio/" — audio assets.
- 🛠️ "Assets/Editor/" — editor-specific tools and scripts.
- ⚙️ "Assets/Settings/" — settings assets.
- 🔤 "Assets/TextMesh Pro/" — text-rendering assets.
- 📦 "Packages/" — Unity package configuration.
- ⚙️ "ProjectSettings/" — Unity project settings.

🌐 Weather Data and Offline Support

AURA-WORLD is designed around two weather-data paths.

🟢 Live Weather Data

Weather information is requested from OpenWeatherMap. This requires a valid API configuration, an available service, and an internet connection.

📴 Cached or Sample Data

Cached responses and a local JSON sample are intended to support testing when a live request cannot be completed.

Expected sample resource path:

"Assets/Resources/sample_weather.json"

The JSON file must exist and contain the fields expected by the application's parser. Offline behaviour should be tested separately from live requests.

🚀 Getting Started

📋 Prerequisites

- 🎮 Unity Hub
- 🧩 A Unity Editor version compatible with the project
- 🔀 Git or GitHub Desktop, or a downloaded copy of the repository
- 🌐 Internet access for live weather requests
- 🔑 A valid OpenWeatherMap API configuration

🪜 Installation Steps

1. 📥 Clone or download this repository.
2. 🎮 Open Unity Hub.
3. 📂 Select Add or Open and choose the project folder.
4. ⏳ Wait for Unity to import assets and resolve packages.
5. 🎬 Open the appropriate existing scene from "Assets/Scenes/".
6. 🛠️ Check the Unity Console for errors or missing references.
7. ▶️ Enter Play mode and validate the interface and environmental behaviour.

Tip: Open the scene containing the AURA-WORLD implementation rather than starting with a new empty scene.

⚙️ Configuration

To configure live weather requests:

1. 🔑 Obtain an OpenWeatherMap API key.
2. 🔍 Locate the weather configuration or request script in the project.
3. ⚙️ Configure the key using the mechanism expected by the code.
4. 🌐 Confirm the API endpoint and expected response fields.
5. 🔎 Test a valid city and an invalid city.
6. 📊 Verify that actual weather values appear in the UI.

🔒 Security reminder: Never commit private credentials to a public repository. API keys included in client-side builds can potentially be extracted. Apply appropriate restrictions and consider a backend if stronger protection is required.

🧪 Testing and Validation

Use this checklist to validate the project:

- [ ] 🎬 Correct Unity scene opens successfully.
- [ ] 🌐 Live weather request succeeds for a valid city.
- [ ] 📊 Weather information appears correctly in the UI.
- [ ] ⚠️ Invalid city names are handled.
- [ ] 🔌 Network and API failures are handled.
- [ ] 💾 Cached weather behaviour is verified.
- [ ] 📴 Sample JSON fallback works without internet access.
- [ ] 🌧️ Rain and no-rain transitions are verified.
- [ ] 💡 Lighting and day/night changes are verified.
- [ ] 🌬️ Wind, fog, and road-material changes are verified where implemented.
- [ ] 📸 Before-and-after screenshots are captured.
- [ ] 🔐 No API credentials are committed.

These are recommended validation checks, not a claim that every test has passed.

📊 Current Status

🚧 Development stage: Prototype and validation

The repository contains the Unity project structure, a 3D city environment, weather-related code, and UI components.

The next validation priorities are:

- 🔗 Confirm scene references and UI connections.
- 🌐 Verify live weather retrieval and displayed values.
- 📴 Validate cached data and sample JSON fallback.
- 🌦️ Confirm that weather values produce the expected scene changes.
- 🧪 Test invalid inputs and API failures.
- 📸 Capture evidence of verified functionality.

Project status should be updated as each behaviour is tested successfully.

🔮 Future Enhancements

Potential improvements include:

- ✨ Smoother transitions between weather states.
- 🌧️ More detailed rain and atmospheric effects.
- 🌇 Improved lighting and environmental realism.
- 🛡️ Stronger data validation and error handling.
- 📊 Clear indicators for live, cached, and sample data.
- 🧪 Automated testing for different weather scenarios.
- 🌐 Optional support for additional weather providers.
- 🎥 A polished demo video and screenshots.
- 🎮 Potential use in simulation, education, and interactive-world development.

These are future possibilities rather than confirmed current features.

👥 Team

🚀 Team IoTrix

AURA-WORLD is a collaborative project exploring how real-world telemetry can be integrated into interactive 3D environments.


📜 License

A license has not yet been specified. Select an appropriate license before allowing others to reuse, modify, or distribute the project.

---

⭐ AURA-WORLD — Bringing real-world weather into virtual environments. 🌦️🏙️
