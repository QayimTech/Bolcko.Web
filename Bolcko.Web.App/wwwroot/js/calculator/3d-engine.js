/**
 * ══════════════════════════════════════════════════════════════════════════════
 * BLOCKO BIM 3D ENGINE 2.0 - PROCEDURAL ARCHITECTURAL & PBR STONE VISUALIZER
 * High-Fidelity Three.js Architectural Visualizer for Jordanian Construction
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
            baseR = 248; baseG = 246; baseB = 242; // Ma'an crystalline white
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
            baseR = 212; baseG = 208; baseB = 202; // Cast stone
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
                    // MUSAMSAM: Chisel comb grooving
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
                    // MONAQQAR: Bush-hammered stipples
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
                    // HONED: Smooth with subtle sedimentary grain
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

        this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
        this.camera.position.set(28, 22, 34);

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
        this.controls.maxPolarAngle = Math.PI / 2 - 0.03; // Don't clip below ground
        this.controls.minDistance = 8;
        this.controls.maxDistance = 120;
        this.controls.target.set(0, 5, 0);

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

        // Grid lines
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

        const side = Math.sqrt(area);
        const width = Math.max(10, side * 0.9);
        const depth = Math.max(10, side * 1.1);
        const floorHeight = 3.6;

        // Get PBR Textures
        const { diffTexture, bumpTexture } = this.textureFactory.getStonePbrTextures(stoneType, stoneFinish);
        const repeatX = Math.max(1, Math.round(width / 4));
        const repeatY = Math.max(1, Math.round((floorHeight * floors) / 3));

        diffTexture.repeat.set(repeatX, repeatY);
        bumpTexture.repeat.set(repeatX, repeatY);

        const stoneMat = new THREE.MeshStandardMaterial({
            map: diffTexture,
            bumpMap: bumpTexture,
            bumpScale: stoneFinish === 'Tabzeh' ? 0.35 : (stoneFinish === 'Mufajjar' ? 0.25 : 0.15),
            roughness: 0.85,
            metalness: 0.05
        });

        const concreteMat = new THREE.MeshStandardMaterial({
            color: 0x64748b,
            roughness: 0.7,
            metalness: 0.1
        });

        const glassMat = new THREE.MeshPhysicalMaterial({
            color: 0x93c5fd,
            transparent: true,
            opacity: 0.45,
            roughness: 0.1,
            metalness: 0.9,
            transmission: 0.7,
            ior: 1.52
        });

        const darkMetalMat = new THREE.MeshStandardMaterial({
            color: 0x1e293b,
            roughness: 0.3,
            metalness: 0.8
        });

        // ════════════ ARCHITECTURAL TYPOLOGY BUILDER ════════════
        if (archStyle === 'classic') {
            this.buildClassicVilla(width, depth, floors, floorHeight, stoneMat, glassMat, darkMetalMat, includeCornice, windowCount, columnCount);
        } else if (archStyle === 'modern') {
            this.buildModernVilla(width, depth, floors, floorHeight, stoneMat, glassMat, darkMetalMat, modernConcept);
        } else if (archStyle === 'l_shape') {
            this.buildLShapeVilla(width, depth, floors, floorHeight, stoneMat, glassMat, darkMetalMat, includeCornice);
        } else {
            this.buildMultiStoryBuilding(width, depth, floors, floorHeight, stoneMat, glassMat, darkMetalMat, includeCornice);
        }

        // Build Skeleton X-Ray
        this.buildStructuralSkeleton(width, depth, floors, floorHeight);

        this.scene.add(this.houseGroup);
        this.scene.add(this.skeletonGroup);

        this.applyXRayVisibility();

        // Update target controls center
        const totalHeight = floorHeight * floors;
        this.controls.target.set(0, totalHeight / 2, 0);

        // Update HUD text
        const overlay = document.getElementById('canvasOverlayInfo');
        if (overlay) {
            const stoneLabels = {
                'Natural_Ruwaished': 'Ruwaished Class A',
                'Natural_Maan': "Ma'an White",
                'Natural_Hayyan': 'Hayyan Mafraq',
                'Natural_Ajloun': 'Ajloun Limestone',
                'Natural_Travertine': 'Travertine Royal',
                'Artificial_HighDensity': 'Cast Stone'
            };
            const styleLabels = {
                'classic': 'Classic Villa',
                'modern': `Modern Villa (${modernConcept})`,
                'l_shape': 'L-Shape Courtyard',
                'apartment': 'Multi-Story'
            };
            overlay.innerText = `${styleLabels[archStyle] || archStyle} • ${floors} Flrs • ${stoneLabels[stoneType] || stoneType}`;
        }
    }

    buildClassicVilla(w, d, floors, fh, stoneMat, glassMat, metalMat, includeCornice, winCount, colCount) {
        for (let f = 0; f < floors; f++) {
            const y = f * fh + fh / 2;

            // Main Stone Volume
            const floorGeo = new THREE.BoxGeometry(w, fh, d);
            const floorMesh = new THREE.Mesh(floorGeo, stoneMat);
            floorMesh.position.y = y;
            floorMesh.castShadow = true;
            floorMesh.receiveShadow = true;
            this.houseGroup.add(floorMesh);

            // Windows with Classical Architraves
            this.addClassicalWindows(w, d, y, fh, glassMat, metalMat);

            // Floor Cornice Stringcourse Belt
            if (includeCornice) {
                const corniceGeo = new THREE.BoxGeometry(w + 0.6, 0.35, d + 0.6);
                const corniceMat = new THREE.MeshStandardMaterial({ color: 0xf1f5f9, roughness: 0.6 });
                const cornice = new THREE.Mesh(corniceGeo, corniceMat);
                cornice.position.y = (f + 1) * fh;
                cornice.castShadow = true;
                this.houseGroup.add(cornice);
            }
        }

        // Classical Entrance Portico with Columns & Pediment
        if (colCount > 0) {
            this.addClassicalPortico(w, d, fh, colCount, stoneMat);
        }

        // Roof Classical Balustrade Parapet
        const parapetH = 0.9;
        const parapetGeo = new THREE.BoxGeometry(w + 0.2, parapetH, d + 0.2);
        const parapetMesh = new THREE.Mesh(parapetGeo, stoneMat);
        parapetMesh.position.y = floors * fh + parapetH / 2;
        parapetMesh.castShadow = true;
        this.houseGroup.add(parapetMesh);
    }

    addClassicalWindows(w, d, y, fh, glassMat, metalMat) {
        const winPositions = [
            { x: -w * 0.28, z: d / 2 + 0.05 },
            { x: w * 0.28, z: d / 2 + 0.05 },
            { x: -w * 0.28, z: -d / 2 - 0.05 },
            { x: w * 0.28, z: -d / 2 - 0.05 }
        ];

        winPositions.forEach(pos => {
            // Glass Pane
            const winGeo = new THREE.BoxGeometry(2.0, 1.8, 0.15);
            const winMesh = new THREE.Mesh(winGeo, glassMat);
            winMesh.position.set(pos.x, y, pos.z);
            this.houseGroup.add(winMesh);

            // Keystone Architrave Frame
            const frameGeo = new THREE.BoxGeometry(2.4, 2.2, 0.25);
            const frameMesh = new THREE.Mesh(frameGeo, metalMat);
            frameMesh.position.set(pos.x, y, pos.z);
            this.houseGroup.add(frameMesh);
        });
    }

    addClassicalPortico(w, d, fh, colCount, stoneMat) {
        const porticoDepth = 2.5;
        const porticoWidth = Math.min(6, w * 0.5);

        // Columns
        const colRadius = 0.25;
        const colH = fh;
        const colGeo = new THREE.CylinderGeometry(colRadius * 0.85, colRadius, colH, 24);

        const colSpacing = porticoWidth / (colCount + 1);
        for (let i = 1; i <= colCount; i++) {
            const cx = -porticoWidth / 2 + i * colSpacing;
            const col = new THREE.Mesh(colGeo, stoneMat);
            col.position.set(cx, colH / 2, d / 2 + porticoDepth);
            col.castShadow = true;
            this.houseGroup.add(col);

            // Capital
            const capGeo = new THREE.BoxGeometry(colRadius * 2.8, 0.25, colRadius * 2.8);
            const cap = new THREE.Mesh(capGeo, stoneMat);
            cap.position.set(cx, colH, d / 2 + porticoDepth);
            this.houseGroup.add(cap);
        }

        // Portico Pediment Roof (Triangular Gable)
        const pedGeo = new THREE.BoxGeometry(porticoWidth + 0.8, 0.4, porticoDepth + 0.8);
        const pedMesh = new THREE.Mesh(pedGeo, stoneMat);
        pedMesh.position.set(0, colH + 0.2, d / 2 + porticoDepth / 2);
        pedMesh.castShadow = true;
        this.houseGroup.add(pedMesh);
    }

    buildModernVilla(w, d, floors, fh, stoneMat, glassMat, metalMat, concept) {
        // High-end Contemporary Architecture: Cantilever, Cubic Dabouq, Horizon Pergola
        for (let f = 0; f < floors; f++) {
            const y = f * fh + fh / 2;
            const isTopFloor = f === floors - 1 && floors > 1;

            // Cantilever Offset Shift
            let shiftX = 0;
            let shiftZ = 0;
            if (isTopFloor) {
                if (concept === 'cantilever') shiftX = 2.2;
                else if (concept === 'cubic') shiftZ = 1.8;
            }

            // Stone Clad Box
            const floorGeo = new THREE.BoxGeometry(w, fh, d);
            const floorMesh = new THREE.Mesh(floorGeo, stoneMat);
            floorMesh.position.set(shiftX, y, shiftZ);
            floorMesh.castShadow = true;
            floorMesh.receiveShadow = true;
            this.houseGroup.add(floorMesh);

            // Full Height Panoramic Ribbon Windows
            const ribbonGeo = new THREE.BoxGeometry(w * 0.65, fh * 0.75, 0.2);
            const ribbonGlass = new THREE.Mesh(ribbonGeo, glassMat);
            ribbonGlass.position.set(shiftX, y, shiftZ + d / 2 + 0.05);
            this.houseGroup.add(ribbonGlass);

            // Modern Dark Bronze Mullions
            const mullionGeo = new THREE.BoxGeometry(w * 0.67, fh * 0.77, 0.25);
            const mullion = new THREE.Mesh(mullionGeo, metalMat);
            mullion.position.set(shiftX, y, shiftZ + d / 2 + 0.02);
            this.houseGroup.add(mullion);

            // Wooden/Dark Louver Pergola (Horizon Concept)
            if (concept === 'horizon' && isTopFloor) {
                const pergolaGeo = new THREE.BoxGeometry(w * 0.8, 0.15, d * 0.5);
                const pergola = new THREE.Mesh(pergolaGeo, metalMat);
                pergola.position.set(shiftX, (f + 1) * fh + 0.1, shiftZ + d * 0.25);
                this.houseGroup.add(pergola);
            }
        }
    }

    buildLShapeVilla(w, d, floors, fh, stoneMat, glassMat, metalMat, includeCornice) {
        // L-Shaped Layout with central courtyard patio
        const wingW = w * 0.55;
        const wingD = d * 0.55;

        for (let f = 0; f < floors; f++) {
            const y = f * fh + fh / 2;

            // Wing A (Main Facade)
            const wingAGeo = new THREE.BoxGeometry(w, fh, wingD);
            const wingA = new THREE.Mesh(wingAGeo, stoneMat);
            wingA.position.set(0, y, -d / 2 + wingD / 2);
            wingA.castShadow = true;
            this.houseGroup.add(wingA);

            // Wing B (Perpendicular Side)
            const wingBGeo = new THREE.BoxGeometry(wingW, fh, d - wingD);
            const wingB = new THREE.Mesh(wingBGeo, stoneMat);
            wingB.position.set(-w / 2 + wingW / 2, y, d / 2 - (d - wingD) / 2);
            wingB.castShadow = true;
            this.houseGroup.add(wingB);

            // Windows looking into courtyard
            const glassGeo = new THREE.BoxGeometry(w * 0.4, fh * 0.65, 0.15);
            const glass = new THREE.Mesh(glassGeo, glassMat);
            glass.position.set(w * 0.15, y, -d / 2 + wingD + 0.05);
            this.houseGroup.add(glass);
        }

        // Courtyard Terrace Deck (Patio)
        const deckGeo = new THREE.BoxGeometry(w - wingW, 0.15, d - wingD);
        const deckMat = new THREE.MeshStandardMaterial({ color: 0x334155, roughness: 0.5 });
        const deck = new THREE.Mesh(deckGeo, deckMat);
        deck.position.set(w / 2 - (w - wingW) / 2, 0.05, d / 2 - (d - wingD) / 2);
        this.houseGroup.add(deck);
    }

    buildMultiStoryBuilding(w, d, floors, fh, stoneMat, glassMat, metalMat, includeCornice) {
        for (let f = 0; f < floors; f++) {
            const y = f * fh + fh / 2;

            // Stone Clad Floor Core
            const floorGeo = new THREE.BoxGeometry(w, fh, d);
            const floorMesh = new THREE.Mesh(floorGeo, stoneMat);
            floorMesh.position.y = y;
            floorMesh.castShadow = true;
            this.houseGroup.add(floorMesh);

            // Balconies with Glass Railings
            const balconyGeo = new THREE.BoxGeometry(w * 0.5, 0.3, 1.6);
            const balconyFloor = new THREE.Mesh(balconyGeo, stoneMat);
            balconyFloor.position.set(0, f * fh + 0.15, d / 2 + 0.8);
            this.houseGroup.add(balconyFloor);

            const glassRailingGeo = new THREE.BoxGeometry(w * 0.5, 0.9, 0.08);
            const glassRailing = new THREE.Mesh(glassRailingGeo, glassMat);
            glassRailing.position.set(0, f * fh + 0.6, d / 2 + 1.55);
            this.houseGroup.add(glassRailing);
        }
    }

    buildStructuralSkeleton(w, d, floors, fh) {
        const rebarMat = new THREE.MeshStandardMaterial({
            color: 0x38bdf8,
            roughness: 0.3,
            metalness: 0.9,
            wireframe: false
        });

        const footingMat = new THREE.MeshStandardMaterial({
            color: 0x475569,
            roughness: 0.8
        });

        // Footings & Ground Beams
        const colsX = 4;
        const colsZ = 4;
        const stepX = (w - 2) / (colsX - 1);
        const stepZ = (d - 2) / (colsZ - 1);

        for (let ix = 0; ix < colsX; ix++) {
            for (let iz = 0; iz < colsZ; iz++) {
                const cx = -w / 2 + 1 + ix * stepX;
                const cz = -d / 2 + 1 + iz * stepZ;

                // Footing Pad (Qawaed)
                const padGeo = new THREE.BoxGeometry(1.6, 0.6, 1.6);
                const pad = new THREE.Mesh(padGeo, footingMat);
                pad.position.set(cx, -0.3, cz);
                this.skeletonGroup.add(pad);

                // Rebar Column Cage
                const colGeo = new THREE.CylinderGeometry(0.2, 0.2, floors * fh, 8);
                const colMesh = new THREE.Mesh(colGeo, rebarMat);
                colMesh.position.set(cx, (floors * fh) / 2, cz);
                this.skeletonGroup.add(colMesh);
            }
        }

        // Floor Concrete Slabs (Uqdat)
        for (let f = 1; f <= floors; f++) {
            const slabGeo = new THREE.BoxGeometry(w + 0.4, 0.3, d + 0.4);
            const slabMesh = new THREE.Mesh(slabGeo, footingMat);
            slabMesh.position.y = f * fh;
            this.skeletonGroup.add(slabMesh);
        }
    }

    toggleXRayMode() {
        this.isXRayMode = !this.isXRayMode;
        this.applyXRayVisibility();

        const btn = document.getElementById('xrayBtn');
        const text = document.getElementById('xrayBtnText');
        if (btn && text) {
            if (this.isXRayMode) {
                btn.className = 'px-3 py-1.5 rounded-xl border border-indigo-500 text-xs font-bold text-white bg-indigo-600 shadow-sm flex items-center gap-1.5 transition-all cursor-pointer';
                text.innerText = 'الوضع الإنشائي (X-Ray ON)';
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
                    child.material.opacity = 0.2;
                    child.material.wireframe = true;
                }
            });
            this.skeletonGroup.visible = true;
        } else {
            this.houseGroup.traverse(child => {
                if (child.isMesh && child.material) {
                    child.material.transparent = child.material.opacity < 0.9;
                    child.material.opacity = 1.0;
                    child.material.wireframe = false;
                }
            });
            this.skeletonGroup.visible = false;
        }
    }

    setCameraPreset(preset) {
        if (!this.camera || !this.controls) return;
        this.isDroneOrbiting = false;

        const targetLook = new THREE.Vector3(0, 4, 0);
        let targetPos = new THREE.Vector3(0, 6, 36);

        if (preset === 'front') {
            targetPos = new THREE.Vector3(0, 5, 38);
        } else if (preset === 'iso') {
            targetPos = new THREE.Vector3(30, 25, 34);
        } else if (preset === 'interior') {
            targetPos = new THREE.Vector3(0, 1.8, 4);
            targetLook.set(0, 1.8, -4);
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
            const radius = 38;
            this.camera.position.x = Math.cos(this.droneAngle) * radius;
            this.camera.position.z = Math.sin(this.droneAngle) * radius;
            this.camera.position.y = 18 + Math.sin(this.droneAngle * 0.5) * 4;
            this.camera.lookAt(this.controls.target);
        } else if (this.isCameraAnimating && this.cameraTargetPos) {
            this.camera.position.lerp(this.cameraTargetPos, 0.06);
            this.controls.target.lerp(this.controlsTargetPos, 0.06);

            if (this.camera.position.distanceTo(this.cameraTargetPos) < 0.1) {
                this.isCameraAnimating = false;
            }
        }

        this.controls.update();
        this.renderer.render(this.scene, this.camera);
    }
}

// Global Export
window.Blocko3DEngine = new ThreeEngine3D();
