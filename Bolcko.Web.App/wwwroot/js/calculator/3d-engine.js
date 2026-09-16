/**
 * ══════════════════════════════════════════════════════════════════════════════
 * BLOCKO BIM 3D ENGINE 2.0 - PROFESSIONAL ARCHITECTURAL & CIVIL ENGINEERING BIM
 * Realistic Revit / AutoCAD / 3ds Max Style High-Fidelity BIM Viewport
 * ══════════════════════════════════════════════════════════════════════════════
 */

class ProceduralTextureFactory {
    constructor() {
        this.cache = new Map();
        this.renderer = null;
    }

    setRenderer(renderer) {
        this.renderer = renderer;
    }

    getStonePbrTextures(stoneType, stoneFinish) {
        const key = `${stoneType}_${stoneFinish}`;
        if (this.cache.has(key)) {
            return this.cache.get(key);
        }

        const TEX_RES = 2048;
        const diffCanvas = document.createElement('canvas');
        diffCanvas.width = TEX_RES;
        diffCanvas.height = TEX_RES;
        const diffCtx = diffCanvas.getContext('2d');

        const bumpCanvas = document.createElement('canvas');
        bumpCanvas.width = TEX_RES;
        bumpCanvas.height = TEX_RES;
        const bumpCtx = bumpCanvas.getContext('2d');

        // Authentic Jordanian stone base palettes
        let baseR = 226, baseG = 210, baseB = 182; // Warm desert limestone (Ruwaished)
        let jointColor = '#16110b';
        let highlightColor = 'rgba(255,255,255,0.70)';
        let shadowColor = 'rgba(18,12,8,0.65)';

        if (stoneType === 'Natural_Maan') {
            baseR = 248; baseG = 246; baseB = 242; // Ma'an pure white
            jointColor = '#2d2720';
            highlightColor = 'rgba(255,255,255,0.85)';
            shadowColor = 'rgba(35,30,24,0.50)';
        } else if (stoneType === 'Natural_Hayyan') {
            baseR = 215; baseG = 198; baseB = 178; // Hayyan hard beige
            jointColor = '#1a1612';
        } else if (stoneType === 'Natural_Ajloun') {
            baseR = 232; baseG = 218; baseB = 190; // Ajloun creamy yellow
            jointColor = '#221c15';
        } else if (stoneType === 'Natural_Travertine') {
            baseR = 216; baseG = 197; baseB = 168; // Jordanian Travertine
            jointColor = '#1e1810';
        } else if (stoneType === 'Artificial_HighDensity') {
            baseR = 212; baseG = 208; baseB = 202; // Cast engineered stone
            jointColor = '#1e1c18';
        }

        // Base fill
        diffCtx.fillStyle = `rgb(${baseR}, ${baseG}, ${baseB})`;
        diffCtx.fillRect(0, 0, TEX_RES, TEX_RES);

        bumpCtx.fillStyle = '#808080';
        bumpCtx.fillRect(0, 0, TEX_RES, TEX_RES);

        // 16 Jordanian standard Madameek courses (25cm per course on 4.0m wall)
        const numCourses = 16;
        const courseHeight = TEX_RES / numCourses; // 128px per course

        for (let r = 0; r < numCourses; r++) {
            const y = r * courseHeight;
            const courseOffset = (r % 2) * 280;
            let x = -courseOffset;

            while (x < TEX_RES + 600) {
                const seed = Math.abs(Math.floor((r * 47.3 + x * 19.7) % 100));
                const blockWidth = 260 + (seed % 200);

                const tint = (seed % 24) - 12;
                const rCol = Math.min(255, Math.max(0, baseR + tint));
                const gCol = Math.min(255, Math.max(0, baseG + tint));
                const bCol = Math.min(255, Math.max(0, baseB + tint));

                // Diffuse base
                diffCtx.fillStyle = `rgb(${rCol}, ${gCol}, ${bCol})`;
                diffCtx.fillRect(x + 4, y + 4, blockWidth - 8, courseHeight - 8);

                // Bump base
                bumpCtx.fillStyle = '#969696';
                bumpCtx.fillRect(x + 4, y + 4, blockWidth - 8, courseHeight - 8);

                // ──────────────── FINISH SCULPTING ────────────────
                if (stoneFinish === 'Tabzeh') {
                    // TABZEH: Draught margin (Safiha) + rock boss (Kousha)
                    const margin = 16;
                    const innerX = x + margin;
                    const innerY = y + margin;
                    const innerW = blockWidth - margin * 2;
                    const innerH = courseHeight - margin * 2;

                    diffCtx.strokeStyle = 'rgba(0,0,0,0.45)';
                    diffCtx.lineWidth = 2.0;
                    diffCtx.strokeRect(innerX, innerY, innerW, innerH);

                    bumpCtx.strokeStyle = '#252525';
                    bumpCtx.lineWidth = 2.5;
                    bumpCtx.strokeRect(innerX, innerY, innerW, innerH);

                    const numFacetsX = 4;
                    const numFacetsY = 2;
                    const facetW = innerW / numFacetsX;
                    const facetH = innerH / numFacetsY;

                    for (let fy = 0; fy < numFacetsY; fy++) {
                        for (let fx = 0; fx < numFacetsX; fx++) {
                            const fcx = innerX + fx * facetW;
                            const fcy = innerY + fy * facetH;
                            const fSeed = (seed * 13 + fx * 17 + fy * 29) % 50;

                            const distFromCenterX = Math.abs((fx + 0.5) - numFacetsX / 2) / (numFacetsX / 2);
                            const distFromCenterY = Math.abs((fy + 0.5) - numFacetsY / 2) / (numFacetsY / 2);
                            const dist = Math.sqrt(distFromCenterX * distFromCenterX + distFromCenterY * distFromCenterY);
                            const peakHeight = Math.max(0, 1.0 - dist * 0.65);

                            const bumpVal = Math.min(255, Math.floor(165 + peakHeight * 85 + (fSeed % 25)));
                            const bHex = bumpVal.toString(16).padStart(2, '0');
                            bumpCtx.fillStyle = `#${bHex}${bHex}${bHex}`;
                            bumpCtx.fillRect(fcx + 1, fcy + 1, facetW - 1, facetH - 1);

                            if ((fx + fy) % 2 === 0) {
                                diffCtx.fillStyle = `rgba(255,255,255,${0.18 + (fSeed % 10) * 0.02})`;
                            } else {
                                diffCtx.fillStyle = `rgba(0,0,0,${0.16 + (fSeed % 10) * 0.02})`;
                            }
                            diffCtx.fillRect(fcx + 1, fcy + 1, facetW - 1, facetH - 1);

                            diffCtx.strokeStyle = 'rgba(0,0,0,0.35)';
                            diffCtx.lineWidth = 1.6;
                            diffCtx.beginPath();
                            diffCtx.moveTo(fcx, fcy);
                            diffCtx.lineTo(fcx + facetW, fcy + facetH * 0.7);
                            diffCtx.stroke();

                            bumpCtx.strokeStyle = '#181818';
                            bumpCtx.lineWidth = 2.0;
                            bumpCtx.beginPath();
                            bumpCtx.moveTo(fcx, fcy);
                            bumpCtx.lineTo(fcx + facetW, fcy + facetH * 0.7);
                            bumpCtx.stroke();
                        }
                    }
                } else if (stoneFinish === 'Musamsam') {
                    // MUSAMSAM: Razor-sharp chisel tooth comb grooving
                    diffCtx.strokeStyle = shadowColor;
                    diffCtx.lineWidth = 1.8;
                    bumpCtx.lineWidth = 2.2;

                    for (let line = 8; line < blockWidth - 8; line += 7.0) {
                        const lx = x + line;
                        diffCtx.beginPath();
                        diffCtx.moveTo(lx, y + 4);
                        diffCtx.lineTo(lx, y + courseHeight - 4);
                        diffCtx.stroke();

                        bumpCtx.strokeStyle = '#0e0e0e';
                        bumpCtx.beginPath();
                        bumpCtx.moveTo(lx, y + 4);
                        bumpCtx.lineTo(lx, y + courseHeight - 4);
                        bumpCtx.stroke();

                        diffCtx.strokeStyle = highlightColor;
                        diffCtx.beginPath();
                        diffCtx.moveTo(lx + 2.0, y + 4);
                        diffCtx.lineTo(lx + 2.0, y + courseHeight - 4);
                        diffCtx.stroke();

                        bumpCtx.strokeStyle = '#ffffff';
                        bumpCtx.beginPath();
                        bumpCtx.moveTo(lx + 2.0, y + 4);
                        bumpCtx.lineTo(lx + 2.0, y + courseHeight - 4);
                        bumpCtx.stroke();

                        diffCtx.strokeStyle = shadowColor;
                    }
                } else if (stoneFinish === 'Monaqqar') {
                    // MONAQQAR: Bush-hammered pyramidal stipples
                    for (let p = 0; p < 220; p++) {
                        const px = x + 5 + ((p * 47 + seed * 9) % (blockWidth - 10));
                        const py = y + 5 + ((p * 73 + seed * 13) % (courseHeight - 10));

                        diffCtx.fillStyle = shadowColor;
                        diffCtx.fillRect(px, py, 2.4, 2.4);

                        bumpCtx.fillStyle = '#0c0c0c';
                        bumpCtx.fillRect(px, py, 2.2, 2.2);

                        diffCtx.fillStyle = highlightColor;
                        diffCtx.fillRect(px + 1.4, py + 1.4, 1.4, 1.4);

                        bumpCtx.fillStyle = '#ffffff';
                        bumpCtx.fillRect(px + 1.4, py + 1.4, 1.6, 1.6);
                    }
                } else if (stoneFinish === 'Honed') {
                    // HONED: Smooth sawn face with sediment veins
                    diffCtx.strokeStyle = 'rgba(150, 130, 100, 0.32)';
                    diffCtx.lineWidth = 1.8;
                    diffCtx.beginPath();
                    const veinStart = y + ((seed * 19) % courseHeight);
                    diffCtx.moveTo(x + 5, veinStart);
                    diffCtx.bezierCurveTo(x + blockWidth * 0.35, veinStart + 10, x + blockWidth * 0.7, veinStart - 8, x + blockWidth - 5, veinStart + 5);
                    diffCtx.stroke();

                    bumpCtx.fillStyle = '#9e9e9e';
                    bumpCtx.fillRect(x + 4, y + 4, blockWidth - 8, courseHeight - 8);
                } else {
                    // MUFAJJAR: Natural rock fracture
                    const numFacets = 3;
                    const fW = (blockWidth - 8) / numFacets;
                    for (let fi = 0; fi < numFacets; fi++) {
                        const fx = x + 4 + fi * fW;
                        const fSeed = (seed * 17 + fi * 31) % 40;
                        bumpCtx.fillStyle = (fSeed % 2 === 0) ? '#d0d0d0' : '#707070';
                        bumpCtx.fillRect(fx, y + 4, fW, courseHeight - 8);

                        bumpCtx.strokeStyle = '#181818';
                        bumpCtx.lineWidth = 2.4;
                        bumpCtx.beginPath();
                        bumpCtx.moveTo(fx, y + 4);
                        bumpCtx.lineTo(fx + fW * 0.45, y + courseHeight - 4);
                        bumpCtx.stroke();

                        diffCtx.strokeStyle = 'rgba(0,0,0,0.38)';
                        diffCtx.lineWidth = 2.0;
                        diffCtx.beginPath();
                        diffCtx.moveTo(fx, y + 4);
                        diffCtx.lineTo(fx + fW * 0.45, y + courseHeight - 4);
                        diffCtx.stroke();
                    }
                }

                // 3D Bevel Edge Chamfers
                diffCtx.fillStyle = highlightColor;
                diffCtx.fillRect(x + 4, y + 4, blockWidth - 8, 3.5);
                diffCtx.fillRect(x + 4, y + 4, 3.5, courseHeight - 8);

                bumpCtx.fillStyle = '#f6f6f6';
                bumpCtx.fillRect(x + 4, y + 4, blockWidth - 8, 3.5);
                bumpCtx.fillRect(x + 4, y + 4, 3.5, courseHeight - 8);

                diffCtx.fillStyle = shadowColor;
                diffCtx.fillRect(x + 4, y + courseHeight - 7.5, blockWidth - 8, 3.5);
                diffCtx.fillRect(x + blockWidth - 7.5, y + 4, 3.5, courseHeight - 8);

                bumpCtx.fillStyle = '#181818';
                bumpCtx.fillRect(x + 4, y + courseHeight - 7.5, blockWidth - 8, 3.5);
                bumpCtx.fillRect(x + blockWidth - 7.5, y + 4, 3.5, courseHeight - 8);

                // Mortar joints
                diffCtx.fillStyle = jointColor;
                diffCtx.fillRect(x + blockWidth - 4.5, y, 7.5, courseHeight);
                bumpCtx.fillStyle = '#000000';
                bumpCtx.fillRect(x + blockWidth - 4.5, y, 7.5, courseHeight);

                x += blockWidth;
            }

            diffCtx.fillStyle = jointColor;
            diffCtx.fillRect(0, y + courseHeight - 4.5, TEX_RES, 7.5);
            bumpCtx.fillStyle = '#000000';
            bumpCtx.fillRect(0, y + courseHeight - 4.5, TEX_RES, 7.5);
        }

        const diffTexture = new THREE.CanvasTexture(diffCanvas);
        diffTexture.wrapS = THREE.RepeatWrapping;
        diffTexture.wrapT = THREE.RepeatWrapping;
        diffTexture.generateMipmaps = true;

        const bumpTexture = new THREE.CanvasTexture(bumpCanvas);
        bumpTexture.wrapS = THREE.RepeatWrapping;
        bumpTexture.wrapT = THREE.RepeatWrapping;
        bumpTexture.generateMipmaps = true;

        const result = { diffTexture, bumpTexture };
        this.cache.set(key, result);
        return result;
    }
}

class LightingEnvironmentManager {
    constructor(scene) {
        this.scene = scene;
        this.currentMode = 'day';
        this.ambientLight = null;
        this.sunLight = null;
        this.fillLight = null;
        this.nightLights = [];
        this.initLights();
    }

    initLights() {
        this.ambientLight = new THREE.AmbientLight(0xffffff, 0.75);
        this.scene.add(this.ambientLight);

        this.sunLight = new THREE.DirectionalLight(0xfff8ee, 1.25);
        this.sunLight.position.set(40, 60, 40);
        this.sunLight.castShadow = true;
        this.sunLight.shadow.mapSize.width = 2048;
        this.sunLight.shadow.mapSize.height = 2048;
        this.sunLight.shadow.camera.near = 0.5;
        this.sunLight.shadow.camera.far = 200;
        this.sunLight.shadow.camera.left = -40;
        this.sunLight.shadow.camera.right = 40;
        this.sunLight.shadow.camera.top = 40;
        this.sunLight.shadow.camera.bottom = -40;
        this.sunLight.shadow.bias = -0.0005;
        this.scene.add(this.sunLight);

        this.fillLight = new THREE.DirectionalLight(0xbfdbfe, 0.45);
        this.fillLight.position.set(-30, 25, -30);
        this.scene.add(this.fillLight);
    }

    setMode(mode) {
        this.currentMode = mode;
        this.clearNightLights();

        if (mode === 'golden') {
            this.ambientLight.color.setHex(0xffe4cc);
            this.ambientLight.intensity = 0.65;

            this.sunLight.color.setHex(0xffaa44);
            this.sunLight.intensity = 1.6;
            this.sunLight.position.set(55, 20, 25);

            this.fillLight.color.setHex(0x60a5fa);
            this.fillLight.intensity = 0.35;
            this.scene.background = new THREE.Color(0x1a1528);
        } else if (mode === 'night') {
            this.ambientLight.color.setHex(0x1e293b);
            this.ambientLight.intensity = 0.35;

            this.sunLight.color.setHex(0x38bdf8);
            this.sunLight.intensity = 0.3;
            this.sunLight.position.set(20, 50, 20);

            this.fillLight.intensity = 0.1;
            this.scene.background = new THREE.Color(0x060913);

            this.addNightArchLights();
        } else {
            // Day mode default
            this.ambientLight.color.setHex(0xffffff);
            this.ambientLight.intensity = 0.75;

            this.sunLight.color.setHex(0xfff8ee);
            this.sunLight.intensity = 1.25;
            this.sunLight.position.set(40, 60, 40);

            this.fillLight.color.setHex(0xbfdbfe);
            this.fillLight.intensity = 0.45;
            this.scene.background = new THREE.Color(0x0f172a);
        }
    }

    addNightArchLights() {
        const warmLed = new THREE.PointLight(0xffb74d, 1.8, 25);
        warmLed.position.set(0, 3.5, 9);
        this.scene.add(warmLed);
        this.nightLights.push(warmLed);

        const spot1 = new THREE.SpotLight(0xffd59e, 2.2, 30, Math.PI / 4, 0.4);
        spot1.position.set(-8, 0.5, 10);
        spot1.target.position.set(-8, 8, 0);
        this.scene.add(spot1);
        this.scene.add(spot1.target);
        this.nightLights.push(spot1, spot1.target);

        const spot2 = new THREE.SpotLight(0xffd59e, 2.2, 30, Math.PI / 4, 0.4);
        spot2.position.set(8, 0.5, 10);
        spot2.target.position.set(8, 8, 0);
        this.scene.add(spot2);
        this.scene.add(spot2.target);
        this.nightLights.push(spot2, spot2.target);
    }

    clearNightLights() {
        this.nightLights.forEach(l => this.scene.remove(l));
        this.nightLights = [];
    }
}

class ThreeEngine3D {
    constructor() {
        this.canvas = null;
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.lightingManager = null;
        this.textureFactory = new ProceduralTextureFactory();

        this.houseGroup = null;
        this.skeletonGroup = null;
        this.isXRayMode = false;
        this.isInitialized = false;

        // Camera animation & Drone Orbit
        this.cameraTargetPos = null;
        this.controlsTargetPos = null;
        this.isCameraAnimating = false;
        this.isDroneOrbiting = false;
        this.droneAngle = 0;

        // Building Settings
        this.currentArchStyle = 'classic';
        this.currentModernConcept = 'cantilever';
    }

    init(canvasId) {
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) return;

        const container = this.canvas.parentElement;
        const width = container.clientWidth || 800;
        const height = container.clientHeight || 480;

        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0f172a);

        this.camera = new THREE.PerspectiveCamera(38, width / height, 0.1, 1000);
        this.camera.position.set(0.2, 5.0, 16.0);

        this.renderer = new THREE.WebGLRenderer({
            canvas: this.canvas,
            antialias: true,
            alpha: false,
            powerPreference: 'high-performance'
        });
        this.renderer.setSize(width, height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.05;

        this.textureFactory.setRenderer(this.renderer);

        this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.maxPolarAngle = Math.PI / 2 - 0.02;
        this.controls.minDistance = 6;
        this.controls.maxDistance = 100;
        this.controls.target.set(0, 3.5, 0);

        this.lightingManager = new LightingEnvironmentManager(this.scene);

        this.createGround();

        window.addEventListener('resize', () => this.onWindowResize());

        this.isInitialized = true;
        this.animate();
    }

    createGround() {
        const groundGeo = new THREE.PlaneGeometry(160, 160);
        const groundMat = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            roughness: 0.9,
            metalness: 0.1
        });
        const ground = new THREE.Mesh(groundGeo, groundMat);
        ground.rotation.x = -Math.PI / 2;
        ground.position.y = -0.05;
        ground.receiveShadow = true;
        this.scene.add(ground);

        // Grid lines (AutoCAD / Revit datum)
        const grid = new THREE.GridHelper(100, 40, 0x334155, 0x1e293b);
        grid.position.y = 0.01;
        this.scene.add(grid);
    }

    onWindowResize() {
        if (!this.canvas || !this.renderer || !this.camera) return;
        const container = this.canvas.parentElement;
        const width = container.clientWidth;
        const height = container.clientHeight;
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    updateScene(params) {
        if (!this.isInitialized) return;

        if (this.houseGroup) this.scene.remove(this.houseGroup);
        if (this.skeletonGroup) this.scene.remove(this.skeletonGroup);

        this.houseGroup = new THREE.Group();
        this.skeletonGroup = new THREE.Group();

        const area = params.area || 250;
        const floors = params.floors || 2;
        const stoneType = params.stoneType || 'Natural_Ruwaished';
        const stoneFinish = params.stoneFinish || 'Mufajjar';
        const stoneEnabled = params.stoneEnabled ?? true;
        const facadesCount = params.facadesCount || 4;
        const includeCornice = params.includeCornice ?? true;
        const windowCount = params.windowCount || 8;
        const columnCount = params.columnCount || 2;
        const archStyle = params.archStyle || this.currentArchStyle;
        const modernConcept = params.modernConcept || this.currentModernConcept;
        const columnBars = params.columnBars || '6Bars';

        const baseWidth = Math.min(10.2, Math.max(7.2, Math.sqrt(area) * 0.45));
        const baseDepth = baseWidth * 0.82;
        const floorHeight = 2.8;

        // PBR Textures with authentic 25cm course mapping
        const { diffTexture, bumpTexture } = this.textureFactory.getStonePbrTextures(stoneType, stoneFinish);
        diffTexture.repeat.set(baseWidth / 4.0, floorHeight / 4.0);
        bumpTexture.repeat.set(baseWidth / 4.0, floorHeight / 4.0);

        const stoneTrimColor = stoneType === "Natural_Maan" ? 0xffffff : (stoneType === "Natural_Ruwaished" ? 0xd8c59f : 0xb5afa3);
        const stonePlinthColor = stoneType === "Natural_Maan" ? 0xd0cbbd : 0x8a7b66;

        const wallMaterial = new THREE.MeshStandardMaterial({
            map: diffTexture,
            bumpMap: bumpTexture,
            bumpScale: stoneFinish === 'Tabzeh' ? 0.042 : (stoneFinish === 'Musamsam' ? 0.032 : (stoneFinish === 'Monaqqar' ? 0.028 : (stoneFinish === 'Honed' ? 0.007 : 0.048))),
            roughness: stoneFinish === 'Tabzeh' ? 0.76 : (stoneFinish === 'Musamsam' ? 0.80 : (stoneFinish === 'Monaqqar' ? 0.78 : (stoneFinish === 'Honed' ? 0.40 : 0.82))),
            metalness: 0.02,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.06 : 1.0
        });

        const stonePlinthMaterial = new THREE.MeshStandardMaterial({
            color: stonePlinthColor,
            roughness: 0.8,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.08 : 1.0
        });

        const stoneTrimMaterial = new THREE.MeshStandardMaterial({
            color: stoneTrimColor,
            roughness: 0.55,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.08 : 1.0
        });

        const floorSlabMaterial = new THREE.MeshStandardMaterial({
            color: 0xded9cf,
            roughness: 0.65,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.12 : 1.0
        });

        // Hyper-realistic Physical Architectural Glass (High Clarity & Fresnel Reflection)
        const glassMaterial = new THREE.MeshPhysicalMaterial({
            color: 0xdbeafe,
            transmission: 0.92,
            opacity: 0.96,
            transparent: true,
            roughness: 0.015,
            ior: 1.52,
            metalness: 0.06,
            clearcoat: 1.0,
            clearcoatRoughness: 0.02,
            depthWrite: false
        });

        // Architectural Thermal-Break Aluminum Profile (Anthracite / Charcoal Metallic)
        const darkMetalMaterial = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            roughness: 0.28,
            metalness: 0.85,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.1 : 1.0
        });

        // Motorized Rolling Shutter Material (شفرات وصندوق أباجور ألمنيوم معزول)
        const shutterSlatsMaterial = new THREE.MeshStandardMaterial({
            color: 0x334155,
            roughness: 0.42,
            metalness: 0.65,
            transparent: this.isXRayMode,
            opacity: this.isXRayMode ? 0.1 : 1.0
        });

        // Sheer Pleated Interior Curtains (ستائر شيفون داخلية مطوية)
        const curtainMaterial = new THREE.MeshStandardMaterial({
            color: 0xf8fafc,
            roughness: 0.9,
            metalness: 0.0,
            transparent: true,
            opacity: 0.88
        });

        // Deep Window Reveal Backing Cavity
        const revealBackMaterial = new THREE.MeshStandardMaterial({
            color: 0x0f172a,
            roughness: 0.95
        });

        const teakLouverMaterial = new THREE.MeshStandardMaterial({
            color: 0x854d0e,
            roughness: 0.5
        });

        // ──────────────── 1. BASE PLINTH COURSE (مدماك السلسال الحجري) ────────────────
        const plinthHeight = 0.42;
        const plinthGeo = new THREE.BoxGeometry(baseWidth + 0.16, plinthHeight, baseDepth + 0.16);
        const plinthMesh = new THREE.Mesh(plinthGeo, stonePlinthMaterial);
        plinthMesh.position.set(0, plinthHeight / 2, 0);
        plinthMesh.receiveShadow = true;
        this.houseGroup.add(plinthMesh);

        // ──────────────── 2. DETAILED ARCHITECTURAL BUILDING ────────────────
        const isClassic = archStyle === 'classic';
        const isApartment = archStyle === 'apartment';
        const isModern = archStyle === 'modern';

        for (let floor = 0; floor < floors; floor++) {
            const floorY = plinthHeight + floor * floorHeight;
            let fW = baseWidth;
            let fD = baseDepth;
            let fX = 0;
            let fZ = 0;

            if (isModern && floor === 1) {
                if (modernConcept === 'cantilever') {
                    fX = 0.75;
                    fZ = 0.35;
                } else if (modernConcept === 'cubic') {
                    fW = baseWidth * 0.88;
                    fX = -0.45;
                } else if (modernConcept === 'horizon') {
                    fW = baseWidth * 1.08;
                    fZ = -0.3;
                }
            } else if (archStyle === 'l_shape' && floor > 0) {
                fW = baseWidth * 0.75;
            }

            // Floor Concrete Slab
            const slabH = 0.28;
            const slabGeo = new THREE.BoxGeometry(fW + 0.12, slabH, fD + 0.12);
            const slabMesh = new THREE.Mesh(slabGeo, floorSlabMaterial);
            slabMesh.position.set(fX, floorY + slabH / 2, fZ);
            slabMesh.castShadow = true;
            slabMesh.receiveShadow = true;
            this.houseGroup.add(slabMesh);

            // Wall Volume
            const wallH = floorHeight - slabH;
            const wallGeo = new THREE.BoxGeometry(fW, wallH, fD);
            const wallMesh = new THREE.Mesh(wallGeo, wallMaterial);
            wallMesh.position.set(fX, floorY + slabH + wallH / 2, fZ);
            wallMesh.castShadow = true;
            wallMesh.receiveShadow = true;
            this.houseGroup.add(wallMesh);

            // Floor Cornice Stringcourse Belt
            if (includeCornice && floor > 0) {
                const corniceH = 0.22;
                const corniceGeo = new THREE.BoxGeometry(fW + 0.36, corniceH, fD + 0.36);
                const corniceMesh = new THREE.Mesh(corniceGeo, stoneTrimMaterial);
                corniceMesh.position.set(fX, floorY + slabH / 2, fZ);
                corniceMesh.castShadow = true;
                this.houseGroup.add(corniceMesh);
            }

            // Windows and Architectural Openings
            const frontZ = fZ + fD / 2 + 0.04;
            const halfW = fW / 2;

            if (floor === 0) {
                // Luxury Modern/Classic Entrance Door
                const doorW = isModern ? 1.4 : 1.3;
                const doorH = 2.45;
                const doorX = isModern ? -halfW * 0.45 : 0;
                const doorY = floorY + slabH + doorH / 2;
                this.buildLuxuryEntranceDoor(doorX, doorY, frontZ, doorW, doorH, darkMetalMaterial, stoneTrimMaterial);

                // Flanking Symmetrical or Asymmetrical Windows
                if (isModern) {
                    this.buildArchitecturalWindow(fX + halfW * 0.45, floorY + floorHeight * 0.52, frontZ, 2.4, 2.05, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, false, true, true);
                } else {
                    const winOffset = Math.min(2.8, halfW * 0.6);
                    this.buildArchitecturalWindow(-winOffset, floorY + floorHeight * 0.55, frontZ, 1.35, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, false, true);
                    this.buildArchitecturalWindow(winOffset, floorY + floorHeight * 0.55, frontZ, 1.35, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, false, true);
                }
            } else {
                // Upper Floor Windows & Balconies
                if (isModern) {
                    if (modernConcept === 'cantilever') {
                        this.buildArchitecturalWindow(fX, floorY + floorHeight * 0.52, frontZ, fW * 0.65, 2.15, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, false, true, false);
                    } else if (modernConcept === 'horizon') {
                        this.buildArchitecturalWindow(fX - 0.8, floorY + floorHeight * 0.52, frontZ, 3.2, 1.95, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, false, true, false);
                        // Teak Louvers
                        const louverGeo = new THREE.BoxGeometry(1.6, 2.0, 0.15);
                        const louverMesh = new THREE.Mesh(louverGeo, teakLouverMaterial);
                        louverMesh.position.set(fX + halfW * 0.55, floorY + floorHeight * 0.52, frontZ + 0.05);
                        this.houseGroup.add(louverMesh);
                    } else {
                        this.buildArchitecturalWindow(fX, floorY + floorHeight * 0.55, frontZ, 1.45, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, false, false, true);
                    }
                } else {
                    const winOffset = Math.min(2.8, halfW * 0.6);
                    this.buildArchitecturalWindow(-winOffset, floorY + floorHeight * 0.55, frontZ, 1.35, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, false, true);
                    this.buildArchitecturalWindow(winOffset, floorY + floorHeight * 0.55, frontZ, 1.35, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, false, true);
                    this.buildArchitecturalWindow(0, floorY + floorHeight * 0.55, frontZ, 1.25, 1.45, 'front', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, false, true);
                }
            }

            // Side Facade Windows
            const sideZ = Math.min(1.8, fD * 0.25);
            this.buildArchitecturalWindow(fX + fW / 2 + 0.04, floorY + floorHeight * 0.55, fZ + sideZ, 1.2, 1.4, 'side-right', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, isModern, true);
            this.buildArchitecturalWindow(fX - fW / 2 - 0.04, floorY + floorHeight * 0.55, fZ + sideZ, 1.2, 1.4, 'side-left', stoneTrimMaterial, glassMaterial, darkMetalMaterial, shutterSlatsMaterial, curtainMaterial, revealBackMaterial, isClassic, isModern, true);
        }

        // Classical Portico with Fluted Columns & Pediment
        if (isClassic && columnCount > 0) {
            this.buildClassicalPortico(baseWidth, baseDepth, floorHeight, columnCount, stoneTrimMaterial);
        }

        // Roof Parapet (سترة السطح)
        const topY = plinthHeight + floors * floorHeight;
        const parapetH = 0.95;
        const parapetGeo = new THREE.BoxGeometry(baseWidth + 0.12, parapetH, baseDepth + 0.12);
        const parapetMesh = new THREE.Mesh(parapetGeo, wallMaterial);
        parapetMesh.position.set(0, topY + parapetH / 2, 0);
        parapetMesh.castShadow = true;
        this.houseGroup.add(parapetMesh);

        // Parapet Stone Coping (طبانة السترة)
        const copingH = 0.14;
        const copingGeo = new THREE.BoxGeometry(baseWidth + 0.28, copingH, baseDepth + 0.28);
        const copingMesh = new THREE.Mesh(copingGeo, stoneTrimMaterial);
        copingMesh.position.set(0, topY + parapetH + copingH / 2, 0);
        copingMesh.castShadow = true;
        this.houseGroup.add(copingMesh);

        // ──────────────── 3. TRUE CIVIL ENGINEERING STRUCTURAL BIM SKELETON ────────────────
        this.buildStructuralSkeleton(floors, floorHeight, baseWidth, baseDepth, columnBars);

        // Furnished Interior Living Room
        this.buildFurnishedInterior(baseWidth, baseDepth, plinthHeight);

        this.scene.add(this.houseGroup);
        this.scene.add(this.skeletonGroup);

        this.applyXRayVisibility();

        // Update Camera & Controls target
        const totalH = plinthHeight + floors * floorHeight + 1.2;
        this.controls.target.set(0, Math.max(2.0, totalH * 0.48), 0);
    }

    buildLuxuryEntranceDoor(doorX, doorY, frontZ, doorW, doorH, darkMetalMat, stoneTrimMat) {
        const doorGroup = new THREE.Group();
        doorGroup.position.set(doorX, doorY, frontZ);

        // Recessed Entry Box
        const recessGeo = new THREE.BoxGeometry(doorW + 0.2, doorH + 0.15, 0.12);
        const recessMesh = new THREE.Mesh(recessGeo, stoneTrimMat);
        doorGroup.add(recessMesh);

        // Wood Door Leaf
        const leafGeo = new THREE.BoxGeometry(doorW * 0.80, doorH * 0.94, 0.08);
        const woodMat = new THREE.MeshStandardMaterial({ color: 0x78350f, roughness: 0.35 });
        const leafMesh = new THREE.Mesh(leafGeo, woodMat);
        leafMesh.position.set(0, 0, 0.04);
        doorGroup.add(leafMesh);

        // Brass Vertical Pull Handle
        const handleGeo = new THREE.CylinderGeometry(0.016, 0.016, 1.2, 16);
        const brassMat = new THREE.MeshStandardMaterial({ color: 0xf59e0b, metalness: 0.9, roughness: 0.2 });
        const handleMesh = new THREE.Mesh(handleGeo, brassMat);
        handleMesh.position.set(-doorW * 0.28, 0, 0.10);
        doorGroup.add(handleMesh);

        // Outer Stone Door Frame
        const casingW = 0.14;
        const lintelGeo = new THREE.BoxGeometry(doorW + casingW * 2, casingW, 0.14);
        const lintelMesh = new THREE.Mesh(lintelGeo, stoneTrimMat);
        lintelMesh.position.set(0, doorH / 2 + casingW / 2, 0.06);
        lintelMesh.castShadow = true;
        doorGroup.add(lintelMesh);

        this.houseGroup.add(doorGroup);
    }

    buildArchitecturalWindow(posX, posY, posZ, width, height, orientation, trimMat, glassMat, metalMat, shutterMat, curtainMat, revealMat, isClassic, isModern, hasShutter = true) {
        const winGroup = new THREE.Group();
        winGroup.position.set(posX, posY, posZ);

        if (orientation === 'side-right') winGroup.rotation.y = Math.PI / 2;
        else if (orientation === 'side-left') winGroup.rotation.y = -Math.PI / 2;

        const casingW = 0.12; // 12cm stone architrave width
        const casingThick = 0.10; // 10cm stone thickness
        const revealDepth = 0.16; // 16cm recessed wall reveal depth

        // ──────────────── 1. RECESSED WALL REVEAL CAVITY (غاطس الشباك في الجدار) ────────────────
        const revealBoxGeo = new THREE.BoxGeometry(width, height, 0.02);
        const revealBackMesh = new THREE.Mesh(revealBoxGeo, revealMat);
        revealBackMesh.position.z = -revealDepth;
        winGroup.add(revealBackMesh);

        // Subtle warm interior glow backing
        const warmBackGeo = new THREE.PlaneGeometry(width * 0.95, height * 0.95);
        const warmBackMesh = new THREE.Mesh(warmBackGeo, new THREE.MeshBasicMaterial({ color: 0xffedd5 }));
        warmBackMesh.position.z = -revealDepth + 0.01;
        winGroup.add(warmBackMesh);

        // Elegant Pleated Interior Curtains (ستائر شيفون داخلية مطوية)
        const curtainGeo = new THREE.BoxGeometry(width * 0.92, height * 0.90, 0.02);
        const curtainMesh = new THREE.Mesh(curtainGeo, curtainMat);
        curtainMesh.position.set(0, 0, -revealDepth + 0.03);
        winGroup.add(curtainMesh);

        // ──────────────── 2. FOUR-PIECE HOLLOW STONE ARCHITRAVE (برواز الحجر المفرغ) ────────────────
        // A. Left Stone Jamb (سلاح حجر أيسر)
        const jambGeo = new THREE.BoxGeometry(casingW, height + casingW * 2, casingThick);
        const leftJamb = new THREE.Mesh(jambGeo, trimMat);
        leftJamb.position.set(-width / 2 - casingW / 2, 0, casingThick / 2);
        leftJamb.castShadow = true;
        winGroup.add(leftJamb);

        // B. Right Stone Jamb (سلاح حجر أيمن)
        const rightJamb = new THREE.Mesh(jambGeo, trimMat);
        rightJamb.position.set(width / 2 + casingW / 2, 0, casingThick / 2);
        rightJamb.castShadow = true;
        winGroup.add(rightJamb);

        // C. Top Stone Lintel (كشفة حجرية علوية)
        const lintelGeo = new THREE.BoxGeometry(width + casingW * 2, casingW, casingThick + 0.02);
        const topLintel = new THREE.Mesh(lintelGeo, trimMat);
        topLintel.position.set(0, height / 2 + casingW / 2, (casingThick + 0.02) / 2);
        topLintel.castShadow = true;
        winGroup.add(topLintel);

        // D. Bottom Stone Sill (برطاش حجري مائل مع تصريف وبروز 8 سم)
        const sillGeo = new THREE.BoxGeometry(width + casingW * 2 + 0.20, 0.10, 0.24);
        const stoneSill = new THREE.Mesh(sillGeo, trimMat);
        stoneSill.position.set(0, -height / 2 - casingW / 2 - 0.03, 0.10);
        stoneSill.rotation.x = 0.04; // Gentle drip slope
        stoneSill.castShadow = true;
        winGroup.add(stoneSill);

        // E. Classic Keystone Arch Crown (حجر تاج/قفل مائل كلاسيكي)
        if (isClassic) {
            const crownGeo = new THREE.BoxGeometry(width + casingW * 2 + 0.14, 0.12, 0.14);
            const crownMesh = new THREE.Mesh(crownGeo, trimMat);
            crownMesh.position.set(0, height / 2 + casingW + 0.06, 0.06);
            crownMesh.castShadow = true;
            winGroup.add(crownMesh);

            const keystoneGeo = new THREE.BoxGeometry(0.20, 0.22, 0.18);
            const keystoneMesh = new THREE.Mesh(keystoneGeo, trimMat);
            keystoneMesh.position.set(0, height / 2 + casingW + 0.08, 0.08);
            keystoneMesh.castShadow = true;
            winGroup.add(keystoneMesh);
        }

        // ──────────────── 3. MOTORIZED ROLLING SHUTTER BOX (صندوق أباجور ألمنيوم معزول علوي) ────────────────
        const shutterHeight = hasShutter ? Math.min(0.38, height * 0.22) : 0;
        const glassHeight = height - shutterHeight;
        const glassCenterY = -shutterHeight / 2;

        if (hasShutter && shutterHeight > 0) {
            // Shutter Box Housing
            const sBoxGeo = new THREE.BoxGeometry(width - 0.02, shutterHeight, 0.12);
            const sBoxMesh = new THREE.Mesh(sBoxGeo, shutterMat);
            sBoxMesh.position.set(0, height / 2 - shutterHeight / 2, -0.02);
            winGroup.add(sBoxMesh);

            // Horizontal Aluminum Slat Grooves
            const numSlats = 5;
            const slatH = shutterHeight / numSlats;
            for (let s = 1; s < numSlats; s++) {
                const slatLineGeo = new THREE.BoxGeometry(width - 0.04, 0.012, 0.125);
                const slatLine = new THREE.Mesh(slatLineGeo, metalMat);
                slatLine.position.set(0, height / 2 - s * slatH, -0.018);
                winGroup.add(slatLine);
            }
        }

        // ──────────────── 4. ALUMINUM THERMAL-BREAK SUBFRAME & SASHES ────────────────
        const frameThick = 0.035;
        const frameDepth = 0.08;

        // Outer Hollow Aluminum Perimeter Subframe
        const frameLeft = new THREE.Mesh(new THREE.BoxGeometry(frameThick, glassHeight, frameDepth), metalMat);
        frameLeft.position.set(-width / 2 + frameThick / 2, glassCenterY, -0.04);
        winGroup.add(frameLeft);

        const frameRight = new THREE.Mesh(new THREE.BoxGeometry(frameThick, glassHeight, frameDepth), metalMat);
        frameRight.position.set(width / 2 - frameThick / 2, glassCenterY, -0.04);
        winGroup.add(frameRight);

        const frameTop = new THREE.Mesh(new THREE.BoxGeometry(width, frameThick, frameDepth), metalMat);
        frameTop.position.set(0, glassCenterY + glassHeight / 2 - frameThick / 2, -0.04);
        winGroup.add(frameTop);

        const frameBottom = new THREE.Mesh(new THREE.BoxGeometry(width, frameThick, frameDepth), metalMat);
        frameBottom.position.set(0, glassCenterY - glassHeight / 2 + frameThick / 2, -0.04);
        winGroup.add(frameBottom);

        // Center Vertical Mullion (قاطع ألمنيوم رأسي)
        const centerMullion = new THREE.Mesh(new THREE.BoxGeometry(0.045, glassHeight, frameDepth), metalMat);
        centerMullion.position.set(0, glassCenterY, -0.04);
        winGroup.add(centerMullion);

        // Metallic Sash Handles (مقابض ألمنيوم للدرفات)
        const handleGeo = new THREE.BoxGeometry(0.015, 0.14, 0.03);
        const handleMesh1 = new THREE.Mesh(handleGeo, metalMat);
        handleMesh1.position.set(-0.04, glassCenterY, -0.01);
        winGroup.add(handleMesh1);

        const handleMesh2 = new THREE.Mesh(handleGeo, metalMat);
        handleMesh2.position.set(0.04, glassCenterY, -0.01);
        winGroup.add(handleMesh2);

        // ──────────────── 5. CRYSTAL-CLEAR DOUBLE GLAZING PANES ────────────────
        const glassGeo = new THREE.BoxGeometry(width - frameThick * 2, glassHeight - frameThick * 2, 0.018);
        const glassMesh = new THREE.Mesh(glassGeo, glassMat);
        glassMesh.position.set(0, glassCenterY, -0.04);
        winGroup.add(glassMesh);

        // Modern Vertical Stone Blades
        if (isModern) {
            const bladeGeo = new THREE.BoxGeometry(0.08, height + 0.3, 0.28);
            const leftBlade = new THREE.Mesh(bladeGeo, trimMat);
            leftBlade.position.set(-width / 2 - casingW - 0.06, 0, 0.12);
            leftBlade.castShadow = true;
            winGroup.add(leftBlade);

            const rightBlade = new THREE.Mesh(bladeGeo, trimMat);
            rightBlade.position.set(width / 2 + casingW + 0.06, 0, 0.12);
            rightBlade.castShadow = true;
            winGroup.add(rightBlade);
        }

        this.houseGroup.add(winGroup);
    }

    buildClassicalPortico(w, d, fh, colCount, trimMat) {
        const porticoW = Math.min(5.2, w * 0.55);
        const porticoD = 2.0;
        const porticoZ = d / 2 + porticoD / 2;

        const colRadius = 0.22;
        const colH = fh - 0.4;
        const colGeo = new THREE.CylinderGeometry(colRadius * 0.88, colRadius, colH, 24);

        const spacing = porticoW / (colCount + 1);
        for (let i = 1; i <= colCount; i++) {
            const cx = -porticoW / 2 + i * spacing;

            const col = new THREE.Mesh(colGeo, trimMat);
            col.position.set(cx, 0.42 + colH / 2, d / 2 + porticoD);
            col.castShadow = true;
            this.houseGroup.add(col);

            // Capital (تاج العمود)
            const cap = new THREE.Mesh(new THREE.BoxGeometry(colRadius * 2.8, 0.22, colRadius * 2.8), trimMat);
            cap.position.set(cx, 0.42 + colH + 0.11, d / 2 + porticoD);
            this.houseGroup.add(cap);
        }

        // Portico Entablature & Pediment (المثلث المعماري الكلاسيكي)
        const pedGeo = new THREE.BoxGeometry(porticoW + 0.6, 0.35, porticoD + 0.6);
        const pedMesh = new THREE.Mesh(pedGeo, trimMat);
        pedMesh.position.set(0, 0.42 + colH + 0.35, porticoZ);
        pedMesh.castShadow = true;
        this.houseGroup.add(pedMesh);
    }

    buildStructuralSkeleton(floors, floorHeight, baseWidth, baseDepth, columnBars) {
        const colTotalH = floors * floorHeight;
        const rebarBarMat = new THREE.MeshStandardMaterial({ color: 0xdc2626, metalness: 0.85, roughness: 0.25 }); // High-tensile steel
        const stirrupMat = new THREE.MeshStandardMaterial({ color: 0xf59e0b, metalness: 0.7, roughness: 0.3 }); // Amber stirrup wire
        const concreteTransMat = new THREE.MeshStandardMaterial({ color: 0x475569, roughness: 0.8, transparent: true, opacity: 0.26 });
        const tieBeamMat = new THREE.MeshStandardMaterial({ color: 0x334155, roughness: 0.85 });

        const colPositions = [
            [-baseWidth * 0.42, -baseDepth * 0.42],
            [baseWidth * 0.42, -baseDepth * 0.42],
            [-baseWidth * 0.42, baseDepth * 0.42],
            [baseWidth * 0.42, baseDepth * 0.42],
            [0, -baseDepth * 0.42],
            [0, baseDepth * 0.42]
        ];

        // 1. ISOLATED FOOTINGS WITH LOWER REBAR MESH (قواعد مسلحة مع فرش وغطاء)
        colPositions.forEach(pos => {
            const footW = 1.35;
            const footH = 0.42;
            const footGeo = new THREE.BoxGeometry(footW, footH, footW);
            const footMesh = new THREE.Mesh(footGeo, concreteTransMat);
            footMesh.position.set(pos[0], footH / 2, pos[1]);
            this.skeletonGroup.add(footMesh);

            // Rebar Mesh (فرش وغطاء)
            for (let m = -0.5; m <= 0.5; m += 0.25) {
                const barX = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, 1.1, 6), rebarBarMat);
                barX.rotation.z = Math.PI / 2;
                barX.position.set(pos[0], 0.08, pos[1] + m);
                this.skeletonGroup.add(barX);

                const barZ = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, 1.1, 6), rebarBarMat);
                barZ.rotation.x = Math.PI / 2;
                barZ.position.set(pos[0] + m, 0.11, pos[1]);
                this.skeletonGroup.add(barZ);
            }

            // Column Neck (رقبة العامود)
            const neckGeo = new THREE.BoxGeometry(0.36, 0.45, 0.36);
            const neckMesh = new THREE.Mesh(neckGeo, concreteTransMat);
            neckMesh.position.set(pos[0], footH + 0.22, pos[1]);
            this.skeletonGroup.add(neckMesh);
        });

        // 2. GROUND TIE BEAMS / SHANNAJAT (الميد والشناجات الأرضية)
        const tieH = 0.38;
        const tieW = 0.30;
        const tieY = 0.42 + tieH / 2;

        [-baseDepth * 0.42, 0, baseDepth * 0.42].forEach(tz => {
            const beamGeo = new THREE.BoxGeometry(baseWidth * 0.84, tieH, tieW);
            const beamMesh = new THREE.Mesh(beamGeo, tieBeamMat);
            beamMesh.position.set(0, tieY, tz);
            this.skeletonGroup.add(beamMesh);
        });

        [-baseWidth * 0.42, 0, baseWidth * 0.42].forEach(tx => {
            const beamGeo = new THREE.BoxGeometry(tieW, tieH, baseDepth * 0.84);
            const beamMesh = new THREE.Mesh(beamGeo, tieBeamMat);
            beamMesh.position.set(tx, tieY, 0);
            this.skeletonGroup.add(beamMesh);
        });

        // 3. REINFORCED COLUMNS WITH LONGITUDINAL BARS & SEISMIC STIRRUPS
        colPositions.forEach(pos => {
            const cGeo = new THREE.BoxGeometry(0.38, colTotalH, 0.38);
            const cMesh = new THREE.Mesh(cGeo, concreteTransMat);
            cMesh.position.set(pos[0], colTotalH / 2, pos[1]);
            this.skeletonGroup.add(cMesh);

            const barCount = columnBars === "8Bars" ? 8 : (columnBars === "10Bars" ? 10 : 6);
            for (let b = 0; b < barCount; b++) {
                const angle = (b / barCount) * Math.PI * 2;
                const r = 0.13;
                const bx = pos[0] + Math.cos(angle) * r;
                const bz = pos[1] + Math.sin(angle) * r;

                // Main Bar
                const barGeo = new THREE.CylinderGeometry(0.014, 0.014, colTotalH + 0.45, 8);
                const barMesh = new THREE.Mesh(barGeo, rebarBarMat);
                barMesh.position.set(bx, (colTotalH + 0.45) / 2, bz);
                this.skeletonGroup.add(barMesh);

                // Starter Dowel Hook (أشاير السطح)
                const hookGeo = new THREE.CylinderGeometry(0.012, 0.012, 0.22, 8);
                const hookMesh = new THREE.Mesh(hookGeo, rebarBarMat);
                hookMesh.rotation.z = Math.PI / 3;
                hookMesh.position.set(bx + 0.08, colTotalH + 0.45, bz);
                this.skeletonGroup.add(hookMesh);
            }

            // Stirrups (الكانات)
            const numStirrups = Math.floor(colTotalH / 0.18);
            for (let s = 0; s < numStirrups; s++) {
                const sy = s * 0.18 + 0.10;
                const ringGeo = new THREE.BoxGeometry(0.30, 0.012, 0.30);
                const ringMesh = new THREE.Mesh(ringGeo, stirrupMat);
                ringMesh.position.set(pos[0], sy, pos[1]);
                this.skeletonGroup.add(ringMesh);
            }
        });

        // 4. RIBBED SLAB JOISTS & HOLLOW BLOCKS (أعصاب السقف وطوب الهوردي)
        for (let f = 1; f <= floors; f++) {
            const slabY = 0.42 + f * floorHeight;
            const joistCount = 7;
            const jSpacing = (baseWidth * 0.8) / (joistCount - 1);
            for (let j = 0; j < joistCount; j++) {
                const jx = -baseWidth * 0.4 + j * jSpacing;
                const jGeo = new THREE.BoxGeometry(0.15, 0.26, baseDepth * 0.84);
                const jMesh = new THREE.Mesh(jGeo, tieBeamMat);
                jMesh.position.set(jx, slabY - 0.13, 0);
                this.skeletonGroup.add(jMesh);

                // Rebar in Joist
                const jBar = new THREE.Mesh(new THREE.CylinderGeometry(0.010, 0.010, baseDepth * 0.84, 6), rebarBarMat);
                jBar.rotation.x = Math.PI / 2;
                jBar.position.set(jx, slabY - 0.20, 0);
                this.skeletonGroup.add(jBar);
            }
        }
    }

    buildFurnishedInterior(baseWidth, baseDepth, plinthHeight) {
        const interiorGroup = new THREE.Group();
        interiorGroup.position.set(0, plinthHeight + 0.02, 0);

        // Living Room Sofa
        const sofaMat = new THREE.MeshStandardMaterial({ color: 0x334155, roughness: 0.6 });
        const sofaBase = new THREE.Mesh(new THREE.BoxGeometry(2.4, 0.45, 0.9), sofaMat);
        sofaBase.position.set(0, 0.22, -baseDepth * 0.2);
        interiorGroup.add(sofaBase);

        const sofaBack = new THREE.Mesh(new THREE.BoxGeometry(2.4, 0.55, 0.25), sofaMat);
        sofaBack.position.set(0, 0.65, -baseDepth * 0.2 - 0.32);
        interiorGroup.add(sofaBack);

        // Marble Coffee Table
        const tableMat = new THREE.MeshStandardMaterial({ color: 0xe2e8f0, roughness: 0.2 });
        const table = new THREE.Mesh(new THREE.BoxGeometry(1.3, 0.35, 0.7), tableMat);
        table.position.set(0, 0.18, -baseDepth * 0.2 + 0.9);
        interiorGroup.add(table);

        this.houseGroup.add(interiorGroup);
    }

    toggleXRayMode() {
        this.isXRayMode = !this.isXRayMode;
        this.applyXRayVisibility();

        const btn = document.getElementById('xrayBtn');
        const text = document.getElementById('xrayBtnText');
        if (btn && text) {
            if (this.isXRayMode) {
                btn.className = 'px-3 py-1.5 rounded-xl border border-indigo-500 text-xs font-bold text-white bg-indigo-600 shadow-sm flex items-center gap-1.5 transition-all cursor-pointer';
                text.innerText = 'عرض الحجر المصمت';
            } else {
                btn.className = 'px-3 py-1.5 rounded-xl border border-slate-700 text-xs font-bold text-slate-200 bg-slate-800 hover:bg-slate-750 shadow-sm flex items-center gap-1.5 transition-all cursor-pointer';
                text.innerText = 'رؤية الهيكل العظم';
            }
        }
    }

    applyXRayVisibility() {
        if (!this.houseGroup || !this.skeletonGroup) return;

        if (this.isXRayMode) {
            this.houseGroup.traverse(child => {
                if (child.isMesh && child.material) {
                    child.material.transparent = true;
                    child.material.opacity = 0.08;
                }
            });
            this.skeletonGroup.visible = true;
        } else {
            this.houseGroup.traverse(child => {
                if (child.isMesh && child.material) {
                    child.material.transparent = child.material.opacity < 0.9;
                    child.material.opacity = 1.0;
                }
            });
            this.skeletonGroup.visible = false;
        }
    }

    setCameraPreset(preset) {
        if (!this.camera || !this.controls) return;
        this.isDroneOrbiting = false;

        const targetLook = new THREE.Vector3(0, 3.5, 0);
        let targetPos = new THREE.Vector3(0.2, 5.0, 16.0);

        if (preset === 'front') {
            targetPos = new THREE.Vector3(0.2, 4.5, 15.5);
        } else if (preset === 'iso') {
            targetPos = new THREE.Vector3(14.0, 11.0, 15.0);
        } else if (preset === 'interior') {
            targetPos = new THREE.Vector3(-1.0, 1.45, 1.6);
            targetLook.set(-0.2, 1.25, -0.6);
        }

        this.cameraTargetPos = targetPos;
        this.controlsTargetPos = targetLook;
        this.isCameraAnimating = true;

        ['front', 'iso', 'interior'].forEach(p => {
            const btn = document.getElementById('cam-btn-' + p);
            if (btn) {
                if (p === preset) {
                    btn.className = 'px-2.5 py-1 rounded-lg bg-indigo-600 text-white font-bold text-[11px] shadow-sm flex items-center gap-1 transition-all cursor-pointer';
                } else {
                    btn.className = 'px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium text-[11px] flex items-center gap-1 transition-all cursor-pointer';
                }
            }
        });
    }

    toggleDroneOrbit() {
        this.isDroneOrbiting = !this.isDroneOrbiting;
        const btn = document.getElementById('droneOrbitBtn');
        if (btn) {
            if (this.isDroneOrbiting) {
                btn.className = 'px-2.5 py-1 rounded-lg bg-emerald-600 text-white font-bold text-[11px] shadow-sm flex items-center gap-1 transition-all cursor-pointer';
            } else {
                btn.className = 'px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium text-[11px] flex items-center gap-1 transition-all cursor-pointer';
            }
        }
    }

    animate() {
        requestAnimationFrame(() => this.animate());

        if (this.isDroneOrbiting) {
            this.droneAngle += 0.008;
            const radius = 22;
            this.camera.position.x = Math.cos(this.droneAngle) * radius;
            this.camera.position.z = Math.sin(this.droneAngle) * radius;
            this.camera.position.y = 8 + Math.sin(this.droneAngle * 0.5) * 2.5;
            this.camera.lookAt(this.controls.target);
        } else if (this.isCameraAnimating && this.cameraTargetPos) {
            this.camera.position.lerp(this.cameraTargetPos, 0.08);
            this.controls.target.lerp(this.controlsTargetPos, 0.08);

            if (this.camera.position.distanceTo(this.cameraTargetPos) < 0.05) {
                this.isCameraAnimating = false;
            }
        }

        this.controls.update();
        this.renderer.render(this.scene, this.camera);
    }
}

// Global Export
window.Blocko3DEngine = new ThreeEngine3D();
