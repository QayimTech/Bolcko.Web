/**
 * ══════════════════════════════════════════════════════════════════════════════
 * BLOCKO CALCULATOR CORE - STATE MANAGEMENT, LIVE BOQ & REACTIVE CONTROLLER
 * Senior Clean Architecture / SOLID decoupled client-side logic
 * ══════════════════════════════════════════════════════════════════════════════
 */

class BlockoCalculatorController {
    constructor() {
        this.isEnMode = false;
        this.debounceTimer = null;
        this.latestCalculationData = null;

        this.config = {
            enableInsulation: false,
            insulationRate: 4.5,
            enablePainting: false,
            paintRate: 3.5,
            enablePlastering: false,
            plasterRate: 4.0,
            enableMEP: false,
            mepRate: 15.0,
            whatsAppNumber: '962790000000'
        };

        this.liveMarketPrices = {
            steel: 515.00,
            cement: 88.00,
            concrete: 43.50,
            blocks: 360.00,
            sand: 14.50
        };

        this.itemDictionary = {
            'Steel': {
                nameEn: 'High-Strength Steel Rebar (Grade 60)',
                nameAr: 'حديد تسليح عالي المقاومة (Grade 60)',
                unitEn: 'Tons',
                unitAr: 'طن',
                noteEn: 'Includes footings, columns, beams & slabs (8-25mm)',
                noteAr: 'يشمل أقطار القواعد والأعمدة والأسقف (8-25 ملم)'
            },
            'Concrete': {
                nameEn: 'Ready-Mix Concrete B250/B300 with Pump',
                nameAr: 'خرسانة جاهزة B250 / B300 مع المضخة',
                unitEn: 'm³',
                unitAr: 'م³',
                noteEn: 'Cast for foundations, beams, columns & slabs with pump',
                noteAr: 'مصبوبة بالقواعد والجسور والأسقف والأعمدة مع نولون المضخة'
            },
            'Cement': {
                nameEn: 'Portland Cement Bags (50 kg)',
                nameAr: 'إسمنت بورتلاندي مكيس 50 كغم',
                unitEn: 'Bags',
                unitAr: 'كيس',
                noteEn: 'For masonry walls, ground sub-base, and plastering',
                noteAr: 'لأعمال البناء، مدات الأرضيات، والقصارة الأولية'
            },
            'Blocks': {
                nameEn: 'Hollow & Rib Concrete Blocks (10/15/20 cm)',
                nameAr: 'طوب إسمنتي مفرغ وهوردي (10/15/20 سم)',
                unitEn: 'Pcs',
                unitAr: 'حبة',
                noteEn: 'For exterior walls, interior partitions, and rib slabs',
                noteAr: 'للجدران الخارجية والقواطع الداخلية وسقف الهوردي'
            },
            'Sand': {
                nameEn: 'Sweileh Sand & Graded Aggregates',
                nameAr: 'رمل صويلح وحصمة سمسمية وعدسية',
                unitEn: 'm³',
                unitAr: 'م³',
                noteEn: 'For mortar mixes, masonry, and ground slabs',
                noteAr: 'لخلطات المونة والبناء والمدات الأرضية'
            },
            'Labor': {
                nameEn: 'Skeleton Contracting & Labor Workforce',
                nameAr: 'أجور عمالة مقاولة العظم والآليات',
                unitEn: 'm² Built-up',
                unitAr: 'م² مسطح',
                noteEn: 'Carpentry, steel fixing, concrete casting, and mason workforce',
                noteAr: 'أجور النجار، الحداد، البناء، ومعدات الحفر والدك'
            },
            'Stone': {
                nameEn: 'Stone Facade Cladding with 7% Wastage',
                nameAr: 'حجر بناء الواجهات مع هالك القص 7%',
                unitEn: 'm² Facade',
                unitAr: 'م² واجهات',
                noteEn: 'Calibrated exterior stone supply delivered to site',
                noteAr: 'توريد حجر بناء معاير ومشذب واصل موقع المشروع'
            },
            'StoneDecor': {
                nameEn: 'Architectural Trim (Cornices & Frames)',
                nameAr: 'الديكورات المعمارية (كرانيش، براويز، أعمدة)',
                unitEn: 'Linear m / Pcs',
                unitAr: 'متر طولي / حبة',
                noteEn: 'Carved or cast architectural details elevating facade aesthetics',
                noteAr: 'تفاصيل معمارية بارزة ترفع القيمة الجمالية للواجهات'
            },
            'StoneLabor': {
                nameEn: 'Stone Mechanical/Wet Fixing & Backing Concrete',
                nameAr: 'مصنعية بناء الحجر وباطون الحشوة والشناكل',
                unitEn: 'm² Fixation',
                unitAr: 'م² تركيب',
                noteEn: 'Masonry craftsmanship, stainless steel anchors, backing cast & mortar',
                noteAr: 'أجور معلم الحجر، باطون الحشوة خلف الحجر، والشناكل المعدنية'
            },
            'AddonInsulation': {
                nameEn: 'Waterproofing & Thermal Insulation Addon',
                nameAr: 'إضافة العزل المائي والحراري للأسطح والقواعد',
                unitEn: 'm²',
                unitAr: 'م²',
                noteEn: 'Bituminous membrane + XPS extruded foam insulation',
                noteAr: 'رولات زفتية عازلة للقواعد والأسطح + فوم عزل حراري'
            },
            'AddonPlastering': {
                nameEn: 'Internal Plastering & Rendering Addon',
                nameAr: 'إضافة أعمال القصارة الداخلية (بؤج وأوتار)',
                unitEn: 'm²',
                unitAr: 'م²',
                noteEn: 'Complete 3-coat internal plastering & finishing',
                noteAr: 'قصارة داخلية 3 أوجه عالية الاستواء والصلابة'
            },
            'AddonPainting': {
                nameEn: 'Deluxe Interior Paint Addon',
                nameAr: 'إضافة الدهانات الداخلية ديلوكس (معجونة + وجهين)',
                unitEn: 'm²',
                unitAr: 'م²',
                noteEn: 'Two coats putty, primer, and super deluxe washable latex paint',
                noteAr: 'معجونة وجهين + سيلر مائي + دهان سوبر ديلوكس قابل للغسيل'
            },
            'AddonMEP': {
                nameEn: 'Electro-Mechanical Infrastructure (MEP) Addon',
                nameAr: 'إضافة التأسيسات الكهروميكانيكية (كهرباء وسباكة)',
                unitEn: 'm²',
                unitAr: 'م²',
                noteEn: 'Sanitary plumbing piping, drainage, water supply, and electrical conduits',
                noteAr: 'تمديدات صحية وتغذية مياه + بايبات وخراطيم ولوحات كهربائية'
            }
        };
    }

    init(options) {
        if (options) {
            this.isEnMode = options.isEnMode || false;
            if (options.config) Object.assign(this.config, options.config);
            if (options.prices) Object.assign(this.liveMarketPrices, options.prices);
        }

        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.init('threeCanvas');
        }

        this.onInputChange();
    }

    // ──────────────── TAB SWITCHING ────────────────
    switchCalcTab(tabName) {
        document.querySelectorAll('.calc-tab-btn').forEach(b => {
            b.className = 'calc-tab-btn flex-1 py-2.5 px-3 rounded-xl text-xs font-semibold transition-all flex items-center justify-center gap-1.5 text-slate-600 hover:text-slate-900 hover:bg-white/60';
        });

        const activeBtn = document.getElementById('tab-btn-' + tabName);
        if (activeBtn) {
            activeBtn.className = 'calc-tab-btn flex-1 py-2.5 px-3 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1.5 bg-white text-slate-900 shadow-sm border border-slate-200/80';
        }

        document.getElementById('tabContentSkeleton')?.classList.add('hidden');
        document.getElementById('tabContentStone')?.classList.add('hidden');
        document.getElementById('tabContentFinishes')?.classList.add('hidden');

        if (tabName === 'skeleton') {
            document.getElementById('tabContentSkeleton')?.classList.remove('hidden');
        } else if (tabName === 'stone') {
            document.getElementById('tabContentStone')?.classList.remove('hidden');
        } else if (tabName === 'finishes') {
            document.getElementById('tabContentFinishes')?.classList.remove('hidden');
        }
    }

    // ──────────────── PARAMETERS & STEPPERS ────────────────
    adjustArea(delta) {
        const input = document.getElementById('areaInput');
        if (!input) return;
        let val = parseInt(input.value) + delta;
        val = Math.max(50, Math.min(1500, val));
        input.value = val;
        this.onInputChange();
    }

    setFloors(n) {
        const input = document.getElementById('floorsInput');
        if (input) input.value = n;
        this.updateFloorButtons(n);
        this.onInputChange();
    }

    updateFloorButtons(activeFloor) {
        document.querySelectorAll('.floor-btn').forEach(btn => {
            btn.className = 'floor-btn py-2 rounded-lg border text-xs font-semibold transition-all bg-white text-slate-700 border-slate-200 hover:bg-slate-50 cursor-pointer';
        });
        const active = document.getElementById('floor-btn-' + activeFloor);
        if (active) {
            active.className = 'floor-btn py-2 rounded-lg border text-xs font-bold transition-all bg-slate-900 text-white border-slate-900 cursor-pointer';
        }
    }

    setStoneFacades(count) {
        const input = document.getElementById('stoneFacadesCount');
        if (input) input.value = count;
        document.querySelectorAll('.facade-btn').forEach(btn => {
            btn.className = 'facade-btn py-2 rounded-lg border text-xs font-semibold bg-white text-slate-700 border-slate-200 hover:bg-slate-50 transition-all cursor-pointer';
        });
        const active = document.getElementById('facade-btn-' + count);
        if (active) {
            active.className = 'facade-btn py-2 rounded-lg border text-xs font-bold bg-slate-900 text-white border-slate-900 transition-all cursor-pointer';
        }
        this.onInputChange();
    }

    adjustWindowFrames(delta) {
        const input = document.getElementById('windowFramesCount');
        if (!input) return;
        let val = Math.max(0, parseInt(input.value || 0) + delta);
        input.value = val;
        const disp = document.getElementById('windowFramesCountDisplay');
        if (disp) disp.innerText = val;
        this.onInputChange();
    }

    adjustEntranceColumns(delta) {
        const input = document.getElementById('entranceColumnsCount');
        if (!input) return;
        let val = Math.max(0, parseInt(input.value || 0) + delta);
        input.value = val;
        const disp = document.getElementById('entranceColumnsCountDisplay');
        if (disp) disp.innerText = val;
        this.onInputChange();
    }

    applyPreset(btn, area, floors, bType, fType, archStyle) {
        document.querySelectorAll('.preset-btn').forEach(b => {
            b.className = 'preset-btn p-2.5 sm:px-3 sm:py-1.5 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-200 border border-slate-700 font-medium transition-all flex items-center justify-center sm:justify-start gap-1.5 text-center sm:text-start';
        });
        if (btn) {
            btn.className = 'preset-btn p-2.5 sm:px-3 sm:py-1.5 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-emerald-400 border border-emerald-500/50 font-bold transition-all flex items-center justify-center sm:justify-start gap-1.5 text-center sm:text-start';
        }

        const areaInput = document.getElementById('areaInput');
        const floorsInput = document.getElementById('floorsInput');
        if (areaInput) areaInput.value = area;
        if (floorsInput) floorsInput.value = floors;

        const bSelect = document.getElementById('buildingTypeSelect');
        if (bSelect) bSelect.value = bType;
        const fSelect = document.getElementById('foundationTypeSelect');
        if (fSelect) fSelect.value = fType;

        if (archStyle) this.setArchitecturalStyle(archStyle);

        this.updateFloorButtons(floors);
        this.onInputChange();
    }

    // ──────────────── ARCHITECTURAL STYLE & CONCEPTS ────────────────
    setArchitecturalStyle(style) {
        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.currentArchStyle = style;
        }

        const styleInput = document.getElementById('architecturalStyle');
        if (styleInput) styleInput.value = style;

        ['classic', 'modern', 'l_shape', 'apartment'].forEach(s => {
            const btn = document.getElementById('arch-btn-' + s);
            if (btn) {
                if (s === style) {
                    btn.className = 'arch-style-btn px-2.5 py-1.5 rounded-lg border text-xs font-bold bg-amber-500 text-slate-950 border-amber-400 shadow-sm transition-all flex items-center justify-center gap-1 cursor-pointer';
                    const icon = btn.querySelector('.material-symbols-outlined');
                    if (icon) icon.className = 'material-symbols-outlined text-sm text-slate-950';
                } else {
                    btn.className = 'arch-style-btn px-2.5 py-1.5 rounded-lg border text-xs font-semibold bg-slate-900 text-slate-300 border-slate-700 hover:border-slate-500 hover:bg-slate-800 transition-all flex items-center justify-center gap-1 cursor-pointer';
                    const icon = btn.querySelector('.material-symbols-outlined');
                    if (icon) icon.className = 'material-symbols-outlined text-sm text-slate-400';
                }
            }
        });

        const modernBar = document.getElementById('modernConceptsContainer');
        if (modernBar) {
            if (style === 'modern') modernBar.classList.remove('hidden');
            else modernBar.classList.add('hidden');
        }

        const labelMap = {
            'classic': this.isEnMode ? "Classic Villa" : "فيلا كلاسيكية",
            'modern': this.isEnMode ? "Modern Villa" : "فيلا مودرن",
            'l_shape': this.isEnMode ? "L-Shape Courtyard" : "فيلا حرف L وفناء",
            'apartment': this.isEnMode ? "Multi-Story Building" : "عمارة متعددة الطوابق"
        };
        const styleLabel = document.getElementById('currentStyleLabel');
        if (styleLabel) styleLabel.innerText = labelMap[style] || style;

        this.trigger3DUpdate();
    }

    setModernConcept(concept) {
        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.currentModernConcept = concept;
        }

        const conceptInput = document.getElementById('modernConcept');
        if (conceptInput) conceptInput.value = concept;

        ['cantilever', 'cubic', 'horizon'].forEach(c => {
            const btn = document.getElementById('concept-btn-' + c);
            if (btn) {
                if (c === concept) {
                    btn.className = 'concept-style-btn px-3 py-1.5 rounded-lg border text-xs font-bold bg-indigo-600 text-white border-indigo-400 shadow-sm transition-all flex items-center justify-center gap-1 cursor-pointer';
                } else {
                    btn.className = 'concept-style-btn px-3 py-1.5 rounded-lg border text-xs font-semibold bg-slate-900 text-slate-300 border-slate-700 hover:border-slate-500 hover:bg-slate-800 transition-all flex items-center justify-center gap-1 cursor-pointer';
                }
            }
        });

        const conceptLabels = {
            'cantilever': this.isEnMode ? "Cantilever Portal" : "كابولي معلق فاخر",
            'cubic': this.isEnMode ? "Cubic Dabouq" : "تكعيبي دابوق",
            'horizon': this.isEnMode ? "Horizon Pergola" : "بانورامي أفقي"
        };
        const cLabel = document.getElementById('currentModernConceptLabel');
        if (cLabel) cLabel.innerText = conceptLabels[concept] || concept;

        this.trigger3DUpdate();
    }

    // ──────────────── LIGHTING & CAMERA CONTROLS ────────────────
    setLightingMode(mode) {
        if (window.Blocko3DEngine && window.Blocko3DEngine.lightingManager) {
            window.Blocko3DEngine.lightingManager.setMode(mode);
        }

        ['day', 'golden', 'night'].forEach(m => {
            const btn = document.getElementById('light-btn-' + m);
            if (btn) {
                if (m === mode) {
                    btn.className = 'light-mode-btn px-2.5 py-1 rounded-lg bg-amber-500 text-slate-950 font-bold text-[11px] flex items-center gap-1 transition-all cursor-pointer';
                } else {
                    btn.className = 'light-mode-btn px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium text-[11px] flex items-center gap-1 transition-all cursor-pointer';
                }
            }
        });
    }

    setCameraPreset(preset) {
        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.setCameraPreset(preset);
        }
    }

    toggleDroneOrbit() {
        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.toggleDroneOrbit();
        }
    }

    toggleXRayMode() {
        if (window.Blocko3DEngine) {
            window.Blocko3DEngine.toggleXRayMode();
        }
    }

    // ──────────────── QUICK CHIP CONTROLLERS ────────────────
    quickSetFinish(finish) {
        const radio = document.querySelector(`input[name="stoneFinish"][value="${finish}"]`);
        if (radio) {
            radio.checked = true;
            this.onInputChange();
        }
    }

    quickSetStone(stone) {
        const radio = document.querySelector(`input[name="stoneType"][value="${stone}"]`);
        if (radio) {
            radio.checked = true;
            this.onInputChange();
        }
    }

    updateFinishQuickButtons(finish) {
        ['Mufajjar', 'Tabzeh', 'Musamsam', 'Monaqqar', 'Honed'].forEach(f => {
            const btn = document.getElementById('quick-finish-' + f);
            if (btn) {
                if (f.toLowerCase() === (finish || '').toLowerCase()) {
                    btn.className = 'quick-finish-chip px-2.5 py-1 rounded-lg text-[11px] font-bold bg-amber-500 text-slate-950 border border-amber-400 shadow-sm flex items-center gap-1 transition-all cursor-pointer';
                } else {
                    btn.className = 'quick-finish-chip px-2.5 py-1 rounded-lg text-[11px] font-semibold bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 hover:border-slate-600 flex items-center gap-1 transition-all cursor-pointer';
                }
            }
        });
    }

    updateTypeQuickButtons(stoneType) {
        ['Natural_Ruwaished', 'Natural_Maan', 'Natural_Travertine', 'Artificial_HighDensity'].forEach(st => {
            const btn = document.getElementById('quick-stone-' + st);
            if (btn) {
                if (st === stoneType) {
                    btn.className = 'quick-stone-chip px-3 py-1 rounded-lg text-xs font-bold bg-indigo-600 text-white border border-indigo-400 shadow-sm flex items-center gap-1 transition-all cursor-pointer';
                } else {
                    btn.className = 'quick-stone-chip px-3 py-1 rounded-lg text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 hover:border-slate-600 flex items-center gap-1 transition-all cursor-pointer';
                }
            }
        });
    }

    // ──────────────── INSTANT MATHEMATICAL EVALUATION ────────────────
    onInputChange() {
        const area = document.getElementById('areaInput')?.value || 250;
        const floors = document.getElementById('floorsInput')?.value || 2;
        
        const areaUnit = this.isEnMode ? ' m²' : ' م²';
        const floorsText = this.isEnMode 
            ? (floors === '1' ? '1 Floor' : floors + ' Floors')
            : (floors === '1' ? ' طابق واحد' : floors + ' طوابق');

        const areaDisp = document.getElementById('areaDisplay');
        if (areaDisp) areaDisp.innerText = area + areaUnit;

        const floorsDisp = document.getElementById('floorsDisplay');
        if (floorsDisp) floorsDisp.innerText = floorsText;

        const stoneFinish = document.querySelector('input[name="stoneFinish"]:checked')?.value || "Mufajjar";
        const stoneType = document.querySelector('input[name="stoneType"]:checked')?.value || "Natural_Ruwaished";

        this.updateFinishQuickButtons(stoneFinish);
        this.updateTypeQuickButtons(stoneType);

        this.calculateInstantLocalMath();
        this.trigger3DUpdate();

        clearTimeout(this.debounceTimer);
        this.debounceTimer = setTimeout(() => {
            this.fetchBackendEstimate();
        }, 250);
    }

    calculateInstantLocalMath() {
        const area = parseFloat(document.getElementById('areaInput')?.value) || 250;
        const floors = parseInt(document.getElementById('floorsInput')?.value) || 1;
        const columnBars = document.querySelector('input[name="columnBarsCount"]:checked')?.value || "6Bars";
        const slabSystem = document.querySelector('input[name="slabSystem"]:checked')?.value || "Ribbed";
        const rebarGrade = document.querySelector('input[name="rebarDensityGrade"]:checked')?.value || "Standard";
        const buildingType = document.getElementById('buildingTypeSelect')?.value || "Residential";
        const foundationType = document.getElementById('foundationTypeSelect')?.value || "IsolatedFootings";
        const citySelect = document.getElementById('citySelect');
        const cityName = citySelect ? citySelect.options[citySelect.selectedIndex].text : '';

        // Stone parameters
        const stoneEnabled = document.getElementById('enableStoneModule')?.checked ?? true;
        const facadesCount = parseInt(document.getElementById('stoneFacadesCount')?.value || 4);
        const stoneType = document.querySelector('input[name="stoneType"]:checked')?.value || "Natural_Ruwaished";
        const stoneFinish = document.querySelector('input[name="stoneFinish"]:checked')?.value || "Mufajjar";
        const includeCornice = document.getElementById('includeCorniceBelt')?.checked ?? true;
        const windowCount = parseInt(document.getElementById('windowFramesCount')?.value || 0);
        const columnCount = parseInt(document.getElementById('entranceColumnsCount')?.value || 0);
        const hybridTrim = document.getElementById('hybridArtificialTrim')?.checked ?? false;

        const totalBuiltUpArea = Math.round(area * floors * 100) / 100;

        // Base Steel calculation
        let baseSteelKg = buildingType === "Commercial" ? 45.0 : 41.0;
        if (rebarGrade === "Economic") baseSteelKg = 37.5;
        else if (rebarGrade === "Heavy") baseSteelKg = 49.0;

        if (columnBars === "8Bars") baseSteelKg += 4.5;
        else if (columnBars === "10Bars") baseSteelKg += 7.5;

        if (slabSystem === "FlatSlab") baseSteelKg += 5.5;
        else if (slabSystem === "Solid") baseSteelKg += 2.0;

        if (foundationType === "Raft") baseSteelKg += 4.0;

        const rebarDisp = document.getElementById('rebarRatioDisplay');
        if (rebarDisp) rebarDisp.innerText = `~${baseSteelKg.toFixed(1)} kg/m²`;

        const steelTons = Math.round(totalBuiltUpArea * (baseSteelKg / 1000.0) * 100) / 100;
        const steelCost = Math.round(steelTons * this.liveMarketPrices.steel);

        let concreteRatio = buildingType === "Commercial" ? 0.42 : 0.38;
        if (slabSystem === "FlatSlab") concreteRatio += 0.05;
        const concreteM3 = Math.round(totalBuiltUpArea * concreteRatio * 100) / 100;
        const concreteCost = Math.round(concreteM3 * this.liveMarketPrices.concrete);

        const cementBags = Math.ceil(totalBuiltUpArea * 0.65);
        const cementCost = Math.round(cementBags * (this.liveMarketPrices.cement / 20.0));

        const blocksCount = Math.ceil(totalBuiltUpArea * (slabSystem === "FlatSlab" ? 12 : 20));
        const blocksCost = Math.round((blocksCount / 1000.0) * this.liveMarketPrices.blocks);

        const sandM3 = Math.round(totalBuiltUpArea * 0.35 * 100) / 100;
        const sandCost = Math.round(sandM3 * this.liveMarketPrices.sand);

        let laborRate = buildingType === "Commercial" ? 38.0 : 34.0;
        if (columnBars === "8Bars") laborRate += 1.5;
        const laborCost = Math.round(totalBuiltUpArea * laborRate);

        const totalSkeletonCost = steelCost + concreteCost + cementCost + blocksCost + sandCost + laborCost;

        // Stone calculation
        let stoneNetArea = 0;
        let stoneMaterialCost = 0;
        let corniceMeters = 0;
        let corniceCost = 0;
        let windowFramesCost = 0;
        let entranceColumnsCost = 0;
        let stoneLaborCost = 0;
        let totalStoneCost = 0;
        let potentialSavings = 0;

        if (stoneEnabled) {
            const side = Math.sqrt(area);
            const fullPerimeter = 4.0 * side;
            const selectedPerimeter = (fullPerimeter / 4.0) * facadesCount;
            const grossArea = selectedPerimeter * (3.3 * floors);
            stoneNetArea = Math.round(grossArea * (1 - 0.18) * 1.07 * 10) / 10;

            const selectedStoneRadio = document.querySelector('input[name="stoneType"]:checked');
            let stoneRate = selectedStoneRadio ? parseFloat(selectedStoneRadio.dataset.price || 18.5) : 18.5;
            let benchmarkNatural = 22.0;

            if (stoneFinish === "Tabzeh") stoneRate += 1.5;
            else if (stoneFinish === "Musamsam") stoneRate += 2.0;
            else if (stoneFinish === "Monaqqar") stoneRate += 2.2;
            else if (stoneFinish === "Honed") stoneRate += 3.0;

            stoneMaterialCost = Math.round(stoneNetArea * stoneRate);

            if (includeCornice && floors > 1) {
                corniceMeters = Math.round(selectedPerimeter * (floors - 1));
                const corniceRate = hybridTrim || stoneType === "Artificial_HighDensity" ? 7.5 : 12.5;
                corniceCost = Math.round(corniceMeters * corniceRate);
            }

            const winRate = hybridTrim || stoneType === "Artificial_HighDensity" ? 22.0 : 38.0;
            windowFramesCost = Math.round(windowCount * winRate);

            const colRate = hybridTrim || stoneType === "Artificial_HighDensity" ? 85.0 : 160.0;
            entranceColumnsCost = Math.round(columnCount * colRate);

            stoneLaborCost = Math.round(stoneNetArea * 12.0);
            totalStoneCost = stoneMaterialCost + corniceCost + windowFramesCost + entranceColumnsCost + stoneLaborCost;

            if (stoneType === "Artificial_HighDensity") {
                potentialSavings = Math.max(0, Math.round((stoneNetArea * benchmarkNatural) - stoneMaterialCost));
            } else if (hybridTrim) {
                const naturalDecor = (corniceMeters * 12.5) + (windowCount * 38.0) + (columnCount * 160.0);
                const hybridDecor = corniceCost + windowFramesCost + entranceColumnsCost;
                potentialSavings = Math.max(0, Math.round(naturalDecor - hybridDecor));
            }
        }

        // Finishing Addons
        let addonInsulationCost = 0;
        let addonPlasteringCost = 0;
        let addonPaintingCost = 0;
        let addonMEPCost = 0;

        const insulationChecked = document.getElementById('addonInsulation')?.checked ?? false;
        if (this.config.enableInsulation && insulationChecked) {
            addonInsulationCost = Math.round(totalBuiltUpArea * this.config.insulationRate);
        }

        const plasteringChecked = document.getElementById('addonPlastering')?.checked ?? false;
        if (this.config.enablePlastering && plasteringChecked) {
            addonPlasteringCost = Math.round(totalBuiltUpArea * this.config.plasterRate);
        }

        const paintingChecked = document.getElementById('addonPainting')?.checked ?? false;
        if (this.config.enablePainting && paintingChecked) {
            addonPaintingCost = Math.round(totalBuiltUpArea * this.config.paintRate);
        }

        const mepChecked = document.getElementById('addonMEP')?.checked ?? false;
        if (this.config.enableMEP && mepChecked) {
            addonMEPCost = Math.round(totalBuiltUpArea * this.config.mepRate);
        }

        const totalAddonsCost = addonInsulationCost + addonPlasteringCost + addonPaintingCost + addonMEPCost;
        const grandTotalCost = totalSkeletonCost + totalStoneCost + totalAddonsCost;
        const costPerM2 = totalBuiltUpArea > 0 ? Math.round((grandTotalCost / totalBuiltUpArea) * 100) / 100 : 0;

        const items = [
            { category: 'Steel', quantity: steelTons, unitPriceJod: this.liveMarketPrices.steel, totalPriceJod: steelCost },
            { category: 'Concrete', quantity: concreteM3, unitPriceJod: this.liveMarketPrices.concrete, totalPriceJod: concreteCost },
            { category: 'Cement', quantity: cementBags, unitPriceJod: this.liveMarketPrices.cement / 20.0, totalPriceJod: cementCost },
            { category: 'Blocks', quantity: blocksCount, unitPriceJod: this.liveMarketPrices.blocks / 1000.0, totalPriceJod: blocksCost },
            { category: 'Sand', quantity: sandM3, unitPriceJod: this.liveMarketPrices.sand, totalPriceJod: sandCost },
            { category: 'Labor', quantity: totalBuiltUpArea, unitPriceJod: laborRate, totalPriceJod: laborCost }
        ];

        if (stoneEnabled) {
            items.push({
                category: 'Stone',
                quantity: stoneNetArea,
                unitPriceJod: stoneNetArea > 0 ? Math.round((stoneMaterialCost / stoneNetArea) * 100) / 100 : 18.5,
                totalPriceJod: stoneMaterialCost
            });
            if (corniceCost + windowFramesCost + entranceColumnsCost > 0) {
                items.push({
                    category: 'StoneDecor',
                    quantity: corniceMeters + windowCount + columnCount,
                    unitPriceJod: 0,
                    totalPriceJod: corniceCost + windowFramesCost + entranceColumnsCost
                });
            }
            items.push({
                category: 'StoneLabor',
                quantity: stoneNetArea,
                unitPriceJod: 12.0,
                totalPriceJod: stoneLaborCost
            });
        }

        if (addonInsulationCost > 0) {
            items.push({
                category: 'AddonInsulation',
                quantity: totalBuiltUpArea,
                unitPriceJod: this.config.insulationRate,
                totalPriceJod: addonInsulationCost
            });
        }
        if (addonPlasteringCost > 0) {
            items.push({
                category: 'AddonPlastering',
                quantity: totalBuiltUpArea,
                unitPriceJod: this.config.plasterRate,
                totalPriceJod: addonPlasteringCost
            });
        }
        if (addonPaintingCost > 0) {
            items.push({
                category: 'AddonPainting',
                quantity: totalBuiltUpArea,
                unitPriceJod: this.config.paintRate,
                totalPriceJod: addonPaintingCost
            });
        }
        if (addonMEPCost > 0) {
            items.push({
                category: 'AddonMEP',
                quantity: totalBuiltUpArea,
                unitPriceJod: this.config.mepRate,
                totalPriceJod: addonMEPCost
            });
        }

        const data = {
            totalBuiltUpArea: totalBuiltUpArea,
            totalSkeletonCostJod: totalSkeletonCost,
            stoneModuleEnabled: stoneEnabled,
            stoneNetAreaM2: stoneNetArea,
            stoneMaterialCostJod: stoneMaterialCost,
            corniceLinearMeters: corniceMeters,
            corniceCostJod: corniceCost,
            windowFramesCostJod: windowFramesCost,
            entranceColumnsCostJod: entranceColumnsCost,
            totalStoneCostJod: totalStoneCost,
            grandTotalCostJod: grandTotalCost,
            potentialSavingsJod: potentialSavings,
            costPerSquareMeterJod: costPerM2,
            steelQuantityTons: steelTons,
            steelCostJod: steelCost,
            concreteCubicMeters: concreteM3,
            concreteCostJod: concreteCost,
            cementBagsCount: cementBags,
            cementCostJod: cementCost,
            masonryBlocksCount: blocksCount,
            masonryBlocksCostJod: blocksCost,
            sandAggregatesCubicMeters: sandM3,
            sandAggregatesCostJod: sandCost,
            estimatedLaborCostJod: laborCost,
            materialItems: items
        };

        this.updateUI(data, area, floors, buildingType, foundationType, cityName, columnBars, stoneType);
    }

    async fetchBackendEstimate() {
        const area = parseFloat(document.getElementById('areaInput')?.value || 250);
        const floors = parseInt(document.getElementById('floorsInput')?.value || 2);
        const buildingType = document.getElementById('buildingTypeSelect')?.value || "Residential";
        const foundationType = document.getElementById('foundationTypeSelect')?.value || "IsolatedFootings";
        const citySelect = document.getElementById('citySelect');
        const city = citySelect ? citySelect.value : "Amman";
        const cityName = citySelect ? citySelect.options[citySelect.selectedIndex].text : "عمان";

        const columnBars = document.querySelector('input[name="columnBarsCount"]:checked')?.value || "6Bars";
        const slabSystem = document.querySelector('input[name="slabSystem"]:checked')?.value || "Ribbed";
        const rebarGrade = document.querySelector('input[name="rebarDensityGrade"]:checked')?.value || "Standard";

        const stoneEnabled = document.getElementById('enableStoneModule')?.checked ?? true;
        const facades = parseInt(document.getElementById('stoneFacadesCount')?.value || 4);
        const stoneType = document.querySelector('input[name="stoneType"]:checked')?.value || "Natural_Ruwaished";
        const stoneFinish = document.querySelector('input[name="stoneFinish"]:checked')?.value || "Mufajjar";
        const includeCornice = document.getElementById('includeCorniceBelt')?.checked ?? true;
        const windowCount = parseInt(document.getElementById('windowFramesCount')?.value || 0);
        const columnCount = parseInt(document.getElementById('entranceColumnsCount')?.value || 0);
        const hybridTrim = document.getElementById('hybridArtificialTrim')?.checked ?? false;

        const payload = {
            builtUpAreaSquareMeters: area,
            numberOfFloors: floors,
            buildingType: buildingType,
            foundationType: foundationType,
            city: city,
            columnBarsCount: columnBars,
            slabSystem: slabSystem,
            rebarDensityGrade: rebarGrade,
            enableStoneModule: stoneEnabled,
            stoneFacadesCount: facades,
            stoneType: stoneType,
            stoneFinish: stoneFinish,
            includeCorniceBelt: includeCornice,
            windowFramesCount: windowCount,
            entranceColumnsCount: columnCount,
            hybridArtificialTrim: hybridTrim,
            includeStoneInstallation: true
        };

        try {
            const res = await fetch('/api/v1/market/calculator/estimate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const json = await res.json();
            if (json.success && json.data) {
                const totalBuiltUpArea = Math.round(area * floors * 100) / 100;
                let addonInsulationCost = 0;
                let addonPlasteringCost = 0;
                let addonPaintingCost = 0;
                let addonMEPCost = 0;

                const insulationChecked = document.getElementById('addonInsulation')?.checked ?? false;
                if (this.config.enableInsulation && insulationChecked) {
                    addonInsulationCost = Math.round(totalBuiltUpArea * this.config.insulationRate);
                    json.data.materialItems.push({
                        category: 'AddonInsulation',
                        quantity: totalBuiltUpArea,
                        unitPriceJod: this.config.insulationRate,
                        totalPriceJod: addonInsulationCost
                    });
                }

                const plasteringChecked = document.getElementById('addonPlastering')?.checked ?? false;
                if (this.config.enablePlastering && plasteringChecked) {
                    addonPlasteringCost = Math.round(totalBuiltUpArea * this.config.plasterRate);
                    json.data.materialItems.push({
                        category: 'AddonPlastering',
                        quantity: totalBuiltUpArea,
                        unitPriceJod: this.config.plasterRate,
                        totalPriceJod: addonPlasteringCost
                    });
                }

                const paintingChecked = document.getElementById('addonPainting')?.checked ?? false;
                if (this.config.enablePainting && paintingChecked) {
                    addonPaintingCost = Math.round(totalBuiltUpArea * this.config.paintRate);
                    json.data.materialItems.push({
                        category: 'AddonPainting',
                        quantity: totalBuiltUpArea,
                        unitPriceJod: this.config.paintRate,
                        totalPriceJod: addonPaintingCost
                    });
                }

                const mepChecked = document.getElementById('addonMEP')?.checked ?? false;
                if (this.config.enableMEP && mepChecked) {
                    addonMEPCost = Math.round(totalBuiltUpArea * this.config.mepRate);
                    json.data.materialItems.push({
                        category: 'AddonMEP',
                        quantity: totalBuiltUpArea,
                        unitPriceJod: this.config.mepRate,
                        totalPriceJod: addonMEPCost
                    });
                }

                const totalAddonsCost = addonInsulationCost + addonPlasteringCost + addonPaintingCost + addonMEPCost;
                if (totalAddonsCost > 0) {
                    json.data.grandTotalCostJod = (json.data.grandTotalCostJod || (json.data.totalSkeletonCostJod + (json.data.totalStoneCostJod || 0))) + totalAddonsCost;
                    json.data.costPerSquareMeterJod = totalBuiltUpArea > 0 ? Math.round((json.data.grandTotalCostJod / totalBuiltUpArea) * 100) / 100 : json.data.costPerSquareMeterJod;
                }

                this.updateUI(json.data, area, floors, buildingType, foundationType, cityName, columnBars, stoneType);
            }
        } catch (err) {
            console.error("Calculator fetch error:", err);
        }
    }

    updateUI(data, area, floors, buildingType, foundationType, cityName, columnBars, stoneType) {
        this.latestCalculationData = data;
        const curr = this.isEnMode ? ' JOD' : ' د.أ';
        const m2Unit = this.isEnMode ? ' m²' : ' م²';
        const costM2Unit = this.isEnMode ? ' JOD/m²' : ' د.أ/م²';
        const tonUnit = this.isEnMode ? 'Tons' : 'طن';
        const m3Unit = this.isEnMode ? 'm³' : 'م³';
        const bagUnit = this.isEnMode ? 'Bags' : 'كيس';
        const pcsUnit = this.isEnMode ? 'Pcs' : 'حبة';

        // Executive Card
        const grandTotal = data.grandTotalCostJod || (data.totalSkeletonCostJod + (data.totalStoneCostJod || 0));
        const grandTotalEl = document.getElementById('grandTotalCost');
        if (grandTotalEl) grandTotalEl.innerText = Math.round(grandTotal).toLocaleString();

        const totalSkelEl = document.getElementById('totalSkeletonCost');
        if (totalSkelEl) totalSkelEl.innerText = Math.round(data.totalSkeletonCostJod).toLocaleString() + curr;

        const stoneCostEl = document.getElementById('totalStoneCost');
        if (stoneCostEl) stoneCostEl.innerText = Math.round(data.totalStoneCostJod || 0).toLocaleString() + curr;

        const stoneSummaryGroup = document.getElementById('stoneSummaryCostGroup');
        if (stoneSummaryGroup) stoneSummaryGroup.style.display = data.stoneModuleEnabled ? 'flex' : 'none';

        const summaryCostPerM2El = document.getElementById('summaryCostPerM2');
        if (summaryCostPerM2El) summaryCostPerM2El.innerText = Math.round(data.costPerSquareMeterJod).toLocaleString() + costM2Unit;

        // Savings Badge
        const savingsBadge = document.getElementById('savingsBadge');
        const savingsText = document.getElementById('savingsBadgeText');
        if (savingsBadge && savingsText) {
            if (data.potentialSavingsJod && data.potentialSavingsJod > 0) {
                savingsBadge.className = 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30';
                savingsText.innerText = this.isEnMode ? `Saved ${Math.round(data.potentialSavingsJod).toLocaleString()} JOD` : `وفرت ${Math.round(data.potentialSavingsJod).toLocaleString()} د.أ`;
            } else {
                savingsBadge.className = 'hidden';
            }
        }

        // Metrics Grid
        const elSteelQty = document.getElementById('metricSteelQty');
        if (elSteelQty) elSteelQty.innerHTML = data.steelQuantityTons.toFixed(1) + ` <span class="text-xs font-normal text-slate-500">${tonUnit}</span>`;
        const elSteelCost = document.getElementById('metricSteelCost');
        if (elSteelCost) elSteelCost.innerText = Math.round(data.steelCostJod).toLocaleString() + curr;

        const elConcQty = document.getElementById('metricConcreteQty');
        if (elConcQty) elConcQty.innerHTML = data.concreteCubicMeters.toFixed(1) + ` <span class="text-xs font-normal text-slate-500">${m3Unit}</span>`;
        const elConcCost = document.getElementById('metricConcreteCost');
        if (elConcCost) elConcCost.innerText = Math.round(data.concreteCostJod).toLocaleString() + curr;

        const elCemQty = document.getElementById('metricCementQty');
        if (elCemQty) elCemQty.innerHTML = data.cementBagsCount + ` <span class="text-xs font-normal text-slate-500">${bagUnit}</span>`;
        const elCemCost = document.getElementById('metricCementCost');
        if (elCemCost) elCemCost.innerText = Math.round(data.cementCostJod).toLocaleString() + curr;

        const elBlkQty = document.getElementById('metricBlocksQty');
        if (elBlkQty) elBlkQty.innerHTML = data.masonryBlocksCount + ` <span class="text-xs font-normal text-slate-500">${pcsUnit}</span>`;
        const elBlkCost = document.getElementById('metricBlocksCost');
        if (elBlkCost) elBlkCost.innerText = Math.round(data.masonryBlocksCostJod).toLocaleString() + curr;

        const elSandQty = document.getElementById('metricSandQty');
        if (elSandQty) elSandQty.innerHTML = data.sandAggregatesCubicMeters.toFixed(1) + ` <span class="text-xs font-normal text-slate-500">${m3Unit}</span>`;
        const elSandCost = document.getElementById('metricSandCost');
        if (elSandCost) elSandCost.innerText = Math.round(data.sandAggregatesCostJod).toLocaleString() + curr;

        const elLaborCost = document.getElementById('metricLaborCost');
        if (elLaborCost) elLaborCost.innerText = Math.round(data.estimatedLaborCostJod).toLocaleString() + curr;

        // Stone Cards
        const cardArea = document.getElementById('cardStoneArea');
        const cardDecor = document.getElementById('cardStoneDecor');
        if (cardArea && cardDecor) {
            if (data.stoneModuleEnabled) {
                cardArea.classList.remove('hidden');
                cardDecor.classList.remove('hidden');
                document.getElementById('metricStoneArea').innerHTML = (data.stoneNetAreaM2 || 0).toFixed(1) + ` <span class="text-xs font-normal text-slate-500">${m2Unit}</span>`;
                document.getElementById('metricStoneCost').innerText = Math.round(data.stoneMaterialCostJod || 0).toLocaleString() + curr;
                
                const decorSum = (data.corniceCostJod || 0) + (data.windowFramesCostJod || 0) + (data.entranceColumnsCostJod || 0);
                document.getElementById('metricDecorSummary').innerHTML = Math.round(decorSum).toLocaleString() + ` <span class="text-xs font-normal text-slate-500">${curr}</span>`;
                document.getElementById('metricDecorDetail').innerText = this.isEnMode 
                    ? `${Math.round(data.corniceLinearMeters || 0)}m Cornice • Frames & Columns`
                    : `${Math.round(data.corniceLinearMeters || 0)}م طولي كرانيش وأعمدة`;
            } else {
                cardArea.classList.add('hidden');
                cardDecor.classList.add('hidden');
            }
        }

        // Progress Bar
        const total = grandTotal || 1;
        const pSteel = ((data.steelCostJod / total) * 100).toFixed(1);
        const pConcrete = ((data.concreteCostJod / total) * 100).toFixed(1);
        const pStone = (((data.totalStoneCostJod || 0) / total) * 100).toFixed(1);
        const pLabor = ((data.estimatedLaborCostJod / total) * 100).toFixed(1);
        const pBlocks = ((data.masonryBlocksCostJod / total) * 100).toFixed(1);
        const pCement = ((data.cementCostJod / total) * 100).toFixed(1);
        const pSand = ((data.sandAggregatesCostJod / total) * 100).toFixed(1);

        document.getElementById('barSteel').style.width = pSteel + '%';
        document.getElementById('barConcrete').style.width = pConcrete + '%';
        document.getElementById('barStone').style.width = pStone + '%';
        document.getElementById('barLabor').style.width = pLabor + '%';
        document.getElementById('barBlocks').style.width = pBlocks + '%';
        document.getElementById('barCement').style.width = pCement + '%';
        document.getElementById('barSand').style.width = pSand + '%';

        const budgetSumText = document.getElementById('budgetSummaryText');
        if (budgetSumText) {
            budgetSumText.innerText = this.isEnMode 
                ? `Steel ${pSteel}% • Concrete ${pConcrete}% • Stone ${pStone}% • Labor ${pLabor}%`
                : `حديد ${pSteel}% • خرسانة ${pConcrete}% • حجر ${pStone}% • عمالة ${pLabor}%`;
        }

        // Screen BOQ Table
        const tbody = document.getElementById('breakdownTbody');
        if (tbody && data.materialItems) {
            let html = '';
            data.materialItems.forEach(item => {
                const dict = this.itemDictionary[item.category] || {};
                const itemName = this.isEnMode ? (dict.nameEn || item.itemNameEn || item.itemNameAr) : (dict.nameAr || item.itemNameAr);
                const itemNote = this.isEnMode ? (dict.noteEn || item.note) : (dict.noteAr || item.note);
                const itemUnit = this.isEnMode ? (dict.unitEn || item.unit) : (dict.unitAr || item.unit);

                html += `
                    <tr class="hover:bg-slate-50/80 transition-colors">
                        <td class="py-3 px-4">
                            <p class="font-bold text-slate-900 text-xs">${itemName}</p>
                            <p class="text-[11px] text-slate-400 mt-0.5">${itemNote}</p>
                        </td>
                        <td class="py-3 px-4 text-center font-bold text-slate-900 text-xs font-mono">
                            ${item.quantity.toFixed(1)} ${itemUnit}
                        </td>
                        <td class="py-3 px-4 text-center font-medium text-slate-600 font-mono">
                            ${item.unitPriceJod > 0 ? item.unitPriceJod.toFixed(2) + ' ' + curr : '—'}
                        </td>
                        <td class="py-3 px-4 text-end font-bold text-slate-900 text-xs font-mono">
                            ${Math.round(item.totalPriceJod).toLocaleString()} ${curr}
                        </td>
                    </tr>
                `;
            });
            tbody.innerHTML = html;
        }

        // Print Report sync
        const pArea = document.getElementById('printArea');
        if (pArea && area) pArea.innerText = area + m2Unit;

        const pFloors = document.getElementById('printFloors');
        if (pFloors && floors) {
            pFloors.innerText = this.isEnMode ? (floors === 1 ? '1 Floor' : floors + ' Floors') : (floors === 1 ? '1 طابق' : floors + ' طوابق');
        }

        const pTotArea = document.getElementById('printTotalArea');
        if (pTotArea) pTotArea.innerText = data.totalBuiltUpArea + m2Unit;

        const pCity = document.getElementById('printCity');
        if (pCity && cityName) pCity.innerText = cityName;

        const pBType = document.getElementById('printBuildingType');
        if (pBType && buildingType) {
            pBType.innerText = this.isEnMode 
                ? (buildingType === 'Residential' ? 'Residential / Villa' : 'Commercial / Offices')
                : (buildingType === 'Residential' ? 'سكني / فيلا' : 'تجاري / مكاتب');
        }

        const pFType = document.getElementById('printFoundationType');
        if (pFType && foundationType) {
            pFType.innerText = this.isEnMode 
                ? (foundationType === 'IsolatedFootings' ? 'Isolated Footings' : 'Raft / Mat Foundation')
                : (foundationType === 'IsolatedFootings' ? 'قواعد منفصلة' : 'لبشة / حصيرة (Raft)');
        }

        const pColBars = document.getElementById('printColumnBars');
        if (pColBars) {
            const colBarsDisplay = columnBars || "6Bars";
            pColBars.innerText = colBarsDisplay === "8Bars" 
                ? (this.isEnMode ? "8 Bars (Reinforced)" : "8 قضبان (تسليح مدعم)")
                : (colBarsDisplay === "10Bars" ? (this.isEnMode ? "10 Bars (Heavy)" : "10 قضبان (أحمال عالية)") : (this.isEnMode ? "6 Bars (Standard)" : "6 قضبان (قياسي)"));
        }

        const pStoneStatus = document.getElementById('printStoneStatus');
        if (pStoneStatus) {
            pStoneStatus.innerText = data.stoneModuleEnabled 
                ? (this.isEnMode ? `Included (${Math.round(data.stoneNetAreaM2)} m² Facades)` : `مشمول (${Math.round(data.stoneNetAreaM2)} م² واجهات)`)
                : (this.isEnMode ? "Skeleton Only" : "عظم فقط");
        }

        const pGrandTotal = document.getElementById('printGrandTotalCost');
        if (pGrandTotal) pGrandTotal.innerText = Math.round(grandTotal).toLocaleString();

        const pSkelCost = document.getElementById('printSkeletonCost');
        if (pSkelCost) pSkelCost.innerText = Math.round(data.totalSkeletonCostJod).toLocaleString() + curr;

        const pStoneCost = document.getElementById('printStoneCost');
        if (pStoneCost) pStoneCost.innerText = Math.round(data.totalStoneCostJod || 0).toLocaleString() + curr;

        const pCostPerM2 = document.getElementById('printCostPerM2');
        if (pCostPerM2) pCostPerM2.innerText = Math.round(data.costPerSquareMeterJod).toLocaleString() + costM2Unit;

        const printTbody = document.getElementById('printBreakdownTbody');
        if (printTbody && data.materialItems) {
            let printHtml = '';
            data.materialItems.forEach(item => {
                const dict = this.itemDictionary[item.category] || {};
                const itemName = this.isEnMode ? (dict.nameEn || item.itemNameEn || item.itemNameAr) : (dict.nameAr || item.itemNameAr);
                const itemNote = this.isEnMode ? (dict.noteEn || item.note) : (dict.noteAr || item.note);
                const itemUnit = this.isEnMode ? (dict.unitEn || item.unit) : (dict.unitAr || item.unit);

                printHtml += `
                    <tr>
                        <td class="py-2 px-3">
                            <strong class="text-slate-900">${itemName}</strong>
                            <span class="text-slate-500 block text-[10px]">${itemNote}</span>
                        </td>
                        <td class="py-2 px-3 text-center font-mono font-bold">${item.quantity.toFixed(1)} ${itemUnit}</td>
                        <td class="py-2 px-3 text-center font-mono">${item.unitPriceJod > 0 ? item.unitPriceJod.toFixed(2) + ' ' + curr : '—'}</td>
                        <td class="py-2 px-3 text-end font-mono font-bold">${Math.round(item.totalPriceJod).toLocaleString()} ${curr}</td>
                    </tr>
                `;
            });
            printTbody.innerHTML = printHtml;
        }
    }

    trigger3DUpdate() {
        if (!window.Blocko3DEngine) return;

        const area = parseFloat(document.getElementById('areaInput')?.value || 250);
        const floors = parseInt(document.getElementById('floorsInput')?.value || 2);
        const stoneType = document.querySelector('input[name="stoneType"]:checked')?.value || "Natural_Ruwaished";
        const stoneFinish = document.querySelector('input[name="stoneFinish"]:checked')?.value || "Mufajjar";
        const stoneEnabled = document.getElementById('enableStoneModule')?.checked ?? true;
        const facadesCount = parseInt(document.getElementById('stoneFacadesCount')?.value || 4);
        const includeCornice = document.getElementById('includeCorniceBelt')?.checked ?? true;
        const windowCount = parseInt(document.getElementById('windowFramesCount')?.value || 8);
        const columnCount = parseInt(document.getElementById('entranceColumnsCount')?.value || 2);
        const archStyle = document.getElementById('architecturalStyle')?.value || "classic";
        const modernConcept = document.getElementById('modernConcept')?.value || "cantilever";

        window.Blocko3DEngine.updateScene({
            area,
            floors,
            stoneType,
            stoneFinish,
            stoneEnabled,
            facadesCount,
            includeCornice,
            windowCount,
            columnCount,
            archStyle,
            modernConcept
        });
    }

    // ──────────────── ACTIONS: PRINT / QUOTE / WHATSAPP ────────────────
    triggerPrintReport() {
        window.print();
    }

    sendToQuote() {
        const quoteItems = [];
        if (this.latestCalculationData && this.latestCalculationData.materialItems && this.latestCalculationData.materialItems.length > 0) {
            this.latestCalculationData.materialItems.forEach(item => {
                const dict = this.itemDictionary[item.category] || {};
                const name = this.isEnMode ? (dict.nameEn || item.category) : (dict.nameAr || item.category);
                const unit = this.isEnMode ? (dict.unitEn || 'Unit') : (dict.unitAr || 'وحدة');
                quoteItems.push({
                    productName: name,
                    quantity: Math.max(1, Math.round(item.quantity * 10) / 10),
                    unit: unit,
                    productId: null,
                    imageUrl: '',
                    sku: ''
                });
            });
        }

        try {
            sessionStorage.setItem('TenderSessionCart', JSON.stringify(quoteItems));
        } catch (e) {
            console.error('Failed to store TenderSessionCart', e);
        }

        window.location.href = '/Shop/Quote/Request';
    }

    shareWhatsApp() {
        const area = document.getElementById('printTotalArea') ? document.getElementById('printTotalArea').innerText : '';
        const grandTotal = document.getElementById('grandTotalCost') ? document.getElementById('grandTotalCost').innerText : '';
        const skeleton = document.getElementById('totalSkeletonCost') ? document.getElementById('totalSkeletonCost').innerText : '';
        const stone = document.getElementById('totalStoneCost') ? document.getElementById('totalStoneCost').innerText : '';
        const steel = document.getElementById('metricSteelQty') ? document.getElementById('metricSteelQty').innerText : '';
        const concrete = document.getElementById('metricConcreteQty') ? document.getElementById('metricConcreteQty').innerText : '';

        const text = this.isEnMode
            ? `Estimated Construction Costs & Quantities via BLOCKO Jordan:%0A` +
              `• Built-up Area: ${area}%0A` +
              `• Grand Total Cost: ${grandTotal} JOD%0A` +
              `• Skeleton: ${skeleton}%0A` +
              `• Stone & Decor: ${stone}%0A` +
              `• Steel Rebar: ${steel}%0A` +
              `• Ready-Mix Concrete: ${concrete}%0A` +
              `Calculator URL: ${encodeURIComponent(window.location.href)}`
            : `حساب كميات وتكاليف البناء والواجهات لمشروعي عبر منصة بلوكو (BLOCKO):%0A` +
              `• مسطح البناء: ${area}%0A` +
              `• التكلفة الإجمالية: ${grandTotal} دينار أردني%0A` +
              `• هيكل العظم: ${skeleton}%0A` +
              `• الحجر والديكورات: ${stone}%0A` +
              `• كمية الحديد: ${steel}%0A` +
              `• كمية الخرسانة: ${concrete}%0A` +
              `رابط الحساب المباشر: ${encodeURIComponent(window.location.href)}`;

        const phone = (this.config.whatsAppNumber || '').replace(/[^0-9]/g, '');
        const waUrl = phone ? `https://api.whatsapp.com/send?phone=${phone}&text=${text}` : `https://api.whatsapp.com/send?text=${text}`;
        window.open(waUrl, '_blank');
    }

    // ──────────────── FAQ ACCORDION ────────────────
    toggleFaq(id) {
        const content = document.getElementById('faq-content-' + id);
        const icon = document.getElementById('faq-icon-' + id);
        const card = document.getElementById('faq-card-' + id);
        if (!content || !icon) return;

        const isHidden = content.classList.contains('hidden');
        if (isHidden) {
            content.classList.remove('hidden');
            icon.classList.add('rotate-180');
            card.classList.remove('bg-slate-50/40', 'border-slate-200');
            card.classList.add('bg-white', 'border-slate-400', 'shadow-sm');
        } else {
            content.classList.add('hidden');
            icon.classList.remove('rotate-180');
            card.classList.remove('bg-white', 'border-slate-400', 'shadow-sm');
            card.classList.add('bg-slate-50/40', 'border-slate-200');
        }
    }

    // ──────────────── SAMPLE BOX MODAL ────────────────
    openSampleBoxModal() {
        const modal = document.getElementById('sampleBoxModal');
        const formContent = document.getElementById('sampleBoxFormContent');
        const successContent = document.getElementById('sampleBoxSuccessContent');
        const titleEl = document.getElementById('modalSampleStoneTitle');

        const selectedStoneRadio = document.querySelector('input[name="stoneType"]:checked');
        const finishRadio = document.querySelector('input[name="stoneFinish"]:checked');
        const stoneVal = selectedStoneRadio ? selectedStoneRadio.value : 'Ruwaished';
        const finishVal = finishRadio ? finishRadio.value : 'Mufajjar';

        if (titleEl) {
            titleEl.innerText = `${stoneVal} (${finishVal})`;
        }

        if (formContent) formContent.classList.remove('hidden');
        if (successContent) successContent.classList.add('hidden');
        if (modal) modal.classList.remove('hidden');
    }

    closeSampleBoxModal() {
        const modal = document.getElementById('sampleBoxModal');
        if (modal) modal.classList.add('hidden');
    }

    async submitSampleBoxOrder() {
        const fullName = document.getElementById('sampleFullName')?.value.trim();
        const phone = document.getElementById('samplePhone')?.value.trim();
        const city = document.getElementById('sampleCity')?.value;
        const address = document.getElementById('sampleAddress')?.value.trim();
        const btn = document.getElementById('submitSampleBtn');

        if (!fullName || !phone || !address) {
            alert(this.isEnMode ? "Please enter your name, phone number, and delivery address." : "يرجى إدخال الاسم الكريم ورقم الهاتف وعنوان الموقع.");
            return;
        }

        const selectedStoneRadio = document.querySelector('input[name="stoneType"]:checked');
        const finishRadio = document.querySelector('input[name="stoneFinish"]:checked');

        const payload = {
            stoneType: selectedStoneRadio ? selectedStoneRadio.value : "Natural_Ruwaished",
            stoneFinish: finishRadio ? finishRadio.value : "Mufajjar",
            fullName: fullName,
            phone: phone,
            city: city,
            deliveryAddress: address,
            samplePriceJod: 5.00
        };

        if (btn) {
            btn.disabled = true;
            btn.innerHTML = `<span class="material-symbols-outlined text-sm animate-spin">progress_activity</span> ${this.isEnMode ? "Processing..." : "جاري التسجيل..."}`;
        }

        try {
            const resp = await fetch('/Shop/Calculator/OrderSampleBox', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const data = await resp.json();

            if (data.success) {
                document.getElementById('sampleBoxFormContent')?.classList.add('hidden');
                document.getElementById('sampleBoxSuccessContent')?.classList.remove('hidden');
                const trackingEl = document.getElementById('sampleTrackingCodeDisplay');
                if (trackingEl) trackingEl.innerText = data.trackingCode || 'SMP-SUCCESS';
            } else {
                alert(data.message || (this.isEnMode ? "Failed to record request." : "حدث خطأ أثناء تسجيل الطلب."));
            }
        } catch (e) {
            console.error(e);
            alert(this.isEnMode ? "Communication error, please try again." : "تعذر الاتصال بالخادم، يرجى المحاولة لاحقاً.");
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = `<span class="material-symbols-outlined text-sm">local_shipping</span> ${this.isEnMode ? "Confirm Sample Request (5 JOD)" : "تأكيد طلب العينة (5 د.أ)"}`;
            }
        }
    }

    // ──────────────── SHOWROOM PASS MODAL ────────────────
    openShowroomPassModal() {
        const modal = document.getElementById('showroomPassModal');
        const formContent = document.getElementById('showroomPassFormContent');
        const resultContent = document.getElementById('showroomPassResultContent');
        const dateInput = document.getElementById('passVisitDate');
        
        if (dateInput && !dateInput.value) {
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            dateInput.value = tomorrow.toISOString().split('T')[0];
        }

        if (formContent) formContent.classList.remove('hidden');
        if (resultContent) resultContent.classList.add('hidden');
        if (modal) modal.classList.remove('hidden');
    }

    closeShowroomPassModal() {
        const modal = document.getElementById('showroomPassModal');
        if (modal) modal.classList.add('hidden');
    }

    async submitShowroomPass() {
        const fullName = document.getElementById('passFullName')?.value.trim();
        const phone = document.getElementById('passPhone')?.value.trim();
        const city = document.getElementById('passCity')?.value;
        const visitDate = document.getElementById('passVisitDate')?.value;
        const areaM2 = document.getElementById('passAreaM2')?.value;
        const btn = document.getElementById('submitPassBtn');

        if (!fullName || !phone) {
            alert(this.isEnMode ? "Please enter your name and phone number." : "يرجى إدخال الاسم الكريم ورقم الهاتف.");
            return;
        }

        const selectedStoneRadio = document.querySelector('input[name="stoneType"]:checked');
        const finishRadio = document.querySelector('input[name="stoneFinish"]:checked');
        const stoneTitle = selectedStoneRadio ? selectedStoneRadio.value : "Natural_Ruwaished";
        const finishTitle = finishRadio ? finishRadio.value : "Mufajjar";

        const payload = {
            stoneType: `${stoneTitle} (${finishTitle})`,
            fullName: fullName,
            phone: phone,
            city: city,
            preferredDate: visitDate ? new Date(visitDate).toISOString() : new Date().toISOString(),
            estimatedAreaM2: areaM2 || "غير محدد"
        };

        if (btn) {
            btn.disabled = true;
            btn.innerHTML = `<span class="material-symbols-outlined text-sm animate-spin">progress_activity</span> ${this.isEnMode ? "Generating..." : "جاري الإصدار..."}`;
        }

        try {
            const resp = await fetch('/Shop/Calculator/RequestShowroomPass', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const data = await resp.json();

            if (data.success) {
                document.getElementById('showroomPassFormContent')?.classList.add('hidden');
                document.getElementById('showroomPassResultContent')?.classList.remove('hidden');
                
                document.getElementById('passResultNumberDisplay').innerText = data.passNumber;
                document.getElementById('passResultNameDisplay').innerText = fullName;
                document.getElementById('passResultStoneDisplay').innerText = payload.stoneType;
                document.getElementById('passResultDateDisplay').innerText = data.preferredDate || visitDate;
            } else {
                alert(data.message || (this.isEnMode ? "Failed to generate pass." : "حدث خطأ أثناء إصدار التذكرة."));
            }
        } catch (e) {
            console.error(e);
            alert(this.isEnMode ? "Communication error, please try again." : "تعذر الاتصال بالخادم، يرجى المحاولة لاحقاً.");
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = `<span class="material-symbols-outlined text-sm">confirmation_number</span> ${this.isEnMode ? "Generate Escrow Pass" : "إصدار التذكرة المشفرة"}`;
            }
        }
    }

    printShowroomPass() {
        const card = document.getElementById('printablePassCard');
        if (!card) return;
        const printWin = window.open('', '', 'width=700,height=600');
        printWin.document.write('<html><head><title>BLOCKO Escrow Showroom Pass</title><link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css"></head><body class="p-8 bg-slate-100 flex items-center justify-center">');
        printWin.document.write(card.outerHTML);
        printWin.document.write('</body></html>');
        printWin.document.close();
        printWin.focus();
        setTimeout(() => { printWin.print(); printWin.close(); }, 500);
    }
}

// Global Export
window.BlockoCalculator = new BlockoCalculatorController();
