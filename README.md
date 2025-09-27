
# ClearMerge

**ClearMerge** es una herramienta integrada en el editor de **Unity3D** que simplifica y optimiza la **resolución de conflictos en proyectos colaborativos**.
Está diseñada para equipos que trabajan con **Git** y enfrentan problemas al fusionar escenas (`.unity`), prefabs, y otros assets.
>[!Important]
> Este proyecto fue desarrollado como **Trabajo de Fin de Grado en la UDC**, y tiene como objetivo ayudar a la comunidad de forma **totalmente gratuita**, para todos mis **compañeros** y **desarrolladores de mundos** :).


---

## ✨ Características

* 🔍 **Detección automática** de conflictos en escenas y assets.
* 👀 **Interfaz visual intuitiva** para comparar versiones en conflicto.
* 🛠️ **Resolución interactiva** de conflictos en GameObjects y propiedades.
* 🧩 **Previsualización en tiempo real** de la escena resultante.
* ⚡ **Integración con Git** (commit & push desde Unity).
---

## 📦 Instalación

1. Descarga el archivo **`ClearMerge.unitypackage`** desde la pestaña de [Releases](../../releases).
2. En tu proyecto de Unity, ve a:
   `Assets > Import Package > Custom Package...`
3. Selecciona el archivo `ClearMerge.unitypackage`.
4. Acepta la importación de todos los archivos.

---

## 🚀 Uso

1. Una vez instalado, accede a la herramienta en el menú de Unity:
   **`Tools > ClearMerge`**.
2. Haz clic en **"Scan Conflicts"** para que ClearMerge detecte automáticamente los conflictos en tu proyecto.
3. Visualiza las versiones en conflicto (Before / After) en paralelo dentro del editor.
4. Selecciona cómo resolver cada conflicto:

   * ✅ Mantener versión **HEAD**
   * ✅ Mantener versión **INCOMING**
   * 🔧 Resolver manualmente
5. Previsualiza el resultado final en la escena antes de confirmarlo.
6. Cuando estés conforme, guarda y ClearMerge aplicará los cambios y actualizará el repositorio Git.

---

## 🛠️ Requisitos

* Unity **2021.3 o superior** (probado en Unity 6).
* Git instalado y configurado en el sistema.

