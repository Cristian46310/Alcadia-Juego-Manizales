# APK Android — Tráfico Manizales

APK local para tablet/teléfono. **No** va a Play Store.

## Requisitos

1. **Unity Hub** → editor **6000.4.4f1** (o la del `ProjectVersion.txt`).
2. Módulo **Android Build Support** (SDK + OpenJDK; NDK si Unity lo pide).
3. Abrir la carpeta **`My project/`** en Unity (no solo la raíz del repo).
4. Mapa y assets presentes en este PC (carpetas locales / `Art/Environment` si aplica).

## Configuración ya aplicada en el proyecto

| Ajuste | Valor |
|--------|--------|
| Nombre app | Tráfico Manizales |
| Package | `com.alcaldia.traficomanizales` |
| Escenas (orden) | Inicio → Test → Taller → MensajeMotivacional 1 |
| Arquitectura | ARM64 |
| Min API | 25 (Android 7.1+) |
| Backend | IL2CPP |
| Entrada activa | **Input System Package** (no "Both") |

## Generar APK desde Unity (recomendado)

1. **File → Build Profiles** → Android → **Switch Platform** (solo la primera vez).
2. Menú **Build → Android → Generar APK (Release)**.
3. El archivo queda en:  
   `My project/Builds/Android/TraficoManizales.apk`

Para pruebas con logs: **Build → Android → Generar APK (Debug)**.

## Generar APK por terminal (opcional)

```bash
/home/cris-grisales/Unity/Hub/Editor/6000.4.4f1/Editor/Unity \
  -batchmode -quit -nographics \
  -projectPath "/home/cris-grisales/Escritorio/Juego-Alcaldia/My project" \
  -executeMethod AndroidBuildHelper.BuildApkRelease \
  -logFile "/home/cris-grisales/Escritorio/Juego-Alcaldia/My project/Builds/android-build.log"
```

## Instalar en tablet

1. Copia `TraficoManizales.apk` a la tablet (USB, Drive, etc.).
2. Permite **instalar apps desconocidas** para el gestor de archivos que uses.
3. Abre el APK e instala.

Con USB y ADB:

```bash
adb install -r "My project/Builds/Android/TraficoManizales.apk"
```

## Probar antes del APK

1. Solo **Inicio.unity** abierta en Hierarchy (cierra otras escenas).
2. Play → **JUGAR** → juego → choque (coche, edificio o barrera) → Taller / Mensaje → volver.

## Si falla la compilación

- **SDK no encontrado:** Edit → Preferences → External Tools → rutas Android SDK/JDK.
- **Escena faltante:** File → Build Settings → las 4 escenas activas, **Inicio** primera.
- **APK sin mapa:** compila en el PC donde el juego se ve completo en el editor.
