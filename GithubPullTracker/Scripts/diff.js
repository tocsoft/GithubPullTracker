/*!

 diff v2.2.2

Software License Agreement (BSD License)

Copyright (c) 2009-2015, Kevin Decker <kpdecker@gmail.com>

All rights reserved.

Redistribution and use of this software in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above
  copyright notice, this list of conditions and the
  following disclaimer.

* Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the
  following disclaimer in the documentation and/or other
  materials provided with the distribution.

* Neither the name of Kevin Decker nor the names of its
  contributors may be used to endorse or promote products
  derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
@license
*/
(function webpackUniversalModuleDefinition(root, factory) {
    if (typeof exports === 'object' && typeof module === 'object')
        module.exports = factory();
    else if (typeof define === 'function' && define.amd)
        define([], factory);
    else if (typeof exports === 'object')
        exports["JsDiff"] = factory();
    else
        root["JsDiff"] = factory();
})(this, function () {
    return (function (modules) { // webpackBootstrap
        	// The module cache
        	var installedModules = {};

        	// The require function
        	function __webpack_require__(moduleId) {

            		// Check if module is in cache
            		if (installedModules[moduleId])
                			return installedModules[moduleId].exports;

            		// Create a new module (and put it into the cache)
            		var module = installedModules[moduleId] = {
                			exports: {},
                			id: moduleId,
                			loaded: false
                
            };

            		// Execute the module function
            		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);

            		// Flag the module as loaded
            		module.loaded = true;

            		// Return the exports of the module
            		return module.exports;
            
        }


        	// expose the modules object (__webpack_modules__)
        	__webpack_require__.m = modules;

        	// expose the module cache
        	__webpack_require__.c = installedModules;

        	// __webpack_public_path__
        	__webpack_require__.p = "";

        	// Load entry module and return exports
        	return __webpack_require__(0);
        
    })
    /************************************************************************/
    ([
    /* 0 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.canonicalize = exports.convertChangesToXML = exports.convertChangesToDMP = exports.parsePatch = exports.applyPatches = exports.applyPatch = exports.createPatch = exports.createTwoFilesPatch = exports.structuredPatch = exports.diffJson = exports.diffCss = exports.diffSentences = exports.diffTrimmedLines = exports.diffLines = exports.diffWordsWithSpace = exports.diffWords = exports.diffChars = exports.Diff = undefined;
        
        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        
        var _character = __webpack_require__(2) ;

        var _word = __webpack_require__(3) ;

        var _line = __webpack_require__(5) ;

        var _sentence = __webpack_require__(6) ;

        var _css = __webpack_require__(7) ;

        var _json = __webpack_require__(8) ;

        var _apply = __webpack_require__(9) ;

        var _parse = __webpack_require__(10) ;

        var _create = __webpack_require__(12) ;

        var _dmp = __webpack_require__(13) ;

        var _xml = __webpack_require__(14) ;

        
        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        /* See LICENSE file for terms of use */

        /*
         * Text diff implementation.
         *
         * This library supports the following APIS:
         * JsDiff.diffChars: Character by character diff
         * JsDiff.diffWords: Word (as defined by \b regex) diff which ignores whitespace
         * JsDiff.diffLines: Line based diff
         *
         * JsDiff.diffCss: Diff targeted at CSS content
         *
         * These methods are based on the implementation proposed in
         * "An O(ND) Difference Algorithm and its Variations" (Myers, 1986).
         * http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.4.6927
         */
        exports. Diff = _base2.default;
        exports. diffChars = _character.diffChars;
        exports. diffWords = _word.diffWords;
        exports. diffWordsWithSpace = _word.diffWordsWithSpace;
        exports. diffLines = _line.diffLines;
        exports. diffTrimmedLines = _line.diffTrimmedLines;
        exports. diffSentences = _sentence.diffSentences;
        exports. diffCss = _css.diffCss;
        exports. diffJson = _json.diffJson;
        exports. structuredPatch = _create.structuredPatch;
        exports. createTwoFilesPatch = _create.createTwoFilesPatch;
        exports. createPatch = _create.createPatch;
        exports. applyPatch = _apply.applyPatch;
        exports. applyPatches = _apply.applyPatches;
        exports. parsePatch = _parse.parsePatch;
        exports. convertChangesToDMP = _dmp.convertChangesToDMP;
        exports. convertChangesToXML = _xml.convertChangesToXML;
        exports. canonicalize = _json.canonicalize;
       
       
    },
    /* 1 */
    function (module, exports) {

        'use strict';

        exports.__esModule = true;
        exports.default = Diff;
        function Diff() { }

        Diff.prototype = { 
            diff: function diff(oldString, newString) {
                var options = arguments.length <= 2 || arguments[2] === undefined ? {} : arguments[2];

                var callback = options.callback;
                if (typeof options === 'function') {
                    callback = options;
                    options = {};
                }
                this.options = options;

                var self = this;

                function done(value) {
                    if (callback) {
                        setTimeout(function () {
                            callback(undefined, value);
                        }, 0);
                        return true;
                    } else {
                        return value;
                    }
                }

                // Allow subclasses to massage the input prior to running
                oldString = this.castInput(oldString);
                newString = this.castInput(newString);

                oldString = this.removeEmpty(this.tokenize(oldString));
                newString = this.removeEmpty(this.tokenize(newString));

                var newLen = newString.length,
                    oldLen = oldString.length;
                var editLength = 1;
                var maxEditLength = newLen + oldLen;
                var bestPath = [{ newPos: -1, components: [] }];

                // Seed editLength = 0, i.e. the content starts with the same values
                var oldPos = this.extractCommon(bestPath[0], newString, oldString, 0);
                if (bestPath[0].newPos + 1 >= newLen && oldPos + 1 >= oldLen) {
                    // Identity per the equality and tokenizer
                    return done([{ value: newString.join(''), count: newString.length }]);
                }

                // Main worker method. checks all permutations of a given edit length for acceptance.
                function execEditLength() {
                    for (var diagonalPath = -1 * editLength; diagonalPath <= editLength; diagonalPath += 2) {
                        var basePath = void 0 ;
                        var addPath = bestPath[diagonalPath - 1],
                            removePath = bestPath[diagonalPath + 1],
                            _oldPos = (removePath ? removePath.newPos : 0) - diagonalPath;
                        if (addPath) {
                            // No one else is going to attempt to use this value, clear it
                            bestPath[diagonalPath - 1] = undefined;
                        }

                        var canAdd = addPath && addPath.newPos + 1 < newLen,
                            canRemove = removePath && 0 <= _oldPos && _oldPos < oldLen;
                        if (!canAdd && !canRemove) {
                            // If this path is a terminal then prune
                            bestPath[diagonalPath] = undefined;
                            continue;
                        }

                        // Select the diagonal that we want to branch from. We select the prior
                        // path whose position in the new string is the farthest from the origin
                        // and does not pass the bounds of the diff graph
                        if (!canAdd || canRemove && addPath.newPos < removePath.newPos) {
                            basePath = clonePath(removePath);
                            self.pushComponent(basePath.components, undefined, true);
                        } else {
                            basePath = addPath; // No need to clone, we've pulled it from the list
                            basePath.newPos++;
                            self.pushComponent(basePath.components, true, undefined);
                        }

                        _oldPos = self.extractCommon(basePath, newString, oldString, diagonalPath);

                        // If we have hit the end of both strings, then we are done
                        if (basePath.newPos + 1 >= newLen && _oldPos + 1 >= oldLen) {
                            return done(buildValues(self, basePath.components, newString, oldString, self.useLongestToken));
                        } else {
                            // Otherwise track this path as a potential candidate and continue.
                            bestPath[diagonalPath] = basePath;
                        }
                    }

                    editLength++;
                }

                // Performs the length of edit iteration. Is a bit fugly as this has to support the
                // sync and async mode which is never fun. Loops over execEditLength until a value
                // is produced.
                if (callback) {
                    (function exec() {
                        setTimeout(function () {
                            // This should not happen, but we want to be safe.
                            /* istanbul ignore next */
                            if (editLength > maxEditLength) {
                                return callback();
                            }

                            if (!execEditLength()) {
                                exec();
                            }
                        }, 0);
                    })();
                } else {
                    while (editLength <= maxEditLength) {
                        var ret = execEditLength();
                        if (ret) {
                            return ret;
                        }
                    }
                }
            },
             pushComponent: function pushComponent(components, added, removed) {
                var last = components[components.length - 1];
                if (last && last.added === added && last.removed === removed) {
                    // We need to clone here as the component clone operation is just
                    // as shallow array clone
                    components[components.length - 1] = { count: last.count + 1, added: added, removed: removed };
                } else {
                    components.push({ count: 1, added: added, removed: removed });
                }
            },
             extractCommon: function extractCommon(basePath, newString, oldString, diagonalPath) {
                var newLen = newString.length,
                    oldLen = oldString.length,
                    newPos = basePath.newPos,
                    oldPos = newPos - diagonalPath,
                    commonCount = 0;
                while (newPos + 1 < newLen && oldPos + 1 < oldLen && this.equals(newString[newPos + 1], oldString[oldPos + 1])) {
                    newPos++;
                    oldPos++;
                    commonCount++;
                }

                if (commonCount) {
                    basePath.components.push({ count: commonCount });
                }

                basePath.newPos = newPos;
                return oldPos;
            },
             equals: function equals(left, right) {
                return left === right;
            },
             removeEmpty: function removeEmpty(array) {
                var ret = [];
                for (var i = 0; i < array.length; i++) {
                    if (array[i]) {
                        ret.push(array[i]);
                    }
                }
                return ret;
            },
             castInput: function castInput(value) {
                return value;
            },
             tokenize: function tokenize(value) {
                return value.split('');
            }
        };

        function buildValues(diff, components, newString, oldString, useLongestToken) {
            var componentPos = 0,
                componentLen = components.length,
                newPos = 0,
                oldPos = 0;

            for (; componentPos < componentLen; componentPos++) {
                var component = components[componentPos];
                if (!component.removed) {
                    if (!component.added && useLongestToken) {
                        var value = newString.slice(newPos, newPos + component.count);
                        value = value.map(function (value, i) {
                            var oldValue = oldString[oldPos + i];
                            return oldValue.length > value.length ? oldValue : value;
                        });

                        component.value = value.join('');
                    } else {
                        component.value = newString.slice(newPos, newPos + component.count).join('');
                    }
                    newPos += component.count;

                    // Common case
                    if (!component.added) {
                        oldPos += component.count;
                    }
                } else {
                    component.value = oldString.slice(oldPos, oldPos + component.count).join('');
                    oldPos += component.count;

                    // Reverse add and remove so removes are output first to match common convention
                    // The diffing algorithm is tied to add then remove output and this is the simplest
                    // route to get the desired output with minimal overhead.
                    if (componentPos && components[componentPos - 1].added) {
                        var tmp = components[componentPos - 1];
                        components[componentPos - 1] = components[componentPos];
                        components[componentPos] = tmp;
                    }
                }
            }

            // Special case handle for when one terminal is ignored. For this case we merge the
            // terminal into the prior string and drop the change.
            var lastComponent = components[componentLen - 1];
            if (componentLen > 1 && (lastComponent.added || lastComponent.removed) && diff.equals('', lastComponent.value)) {
                components[componentLen - 2].value += lastComponent.value;
                components.pop();
            }

            return components;
        }

        function clonePath(path) {
            return { newPos: path.newPos, components: path.components.slice(0) };
        }
        


       
    },
    /* 2 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.characterDiff = undefined;
        exports. diffChars = diffChars;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        var characterDiff = exports. characterDiff = new _base2.default() ;
        function diffChars(oldStr, newStr, callback) {
            return characterDiff.diff(oldStr, newStr, callback);
        }
        


       
    },
    /* 3 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.wordDiff = undefined;
        exports. diffWords = diffWords;
        exports. diffWordsWithSpace = diffWordsWithSpace;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        
        var _params = __webpack_require__(4) ;

        
        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        

        // Based on https://en.wikipedia.org/wiki/Latin_script_in_Unicode
        //
        // Ranges and exceptions:
        // Latin-1 Supplement, 0080–00FF
        //  - U+00D7  × Multiplication sign
        //  - U+00F7  ÷ Division sign
        // Latin Extended-A, 0100–017F
        // Latin Extended-B, 0180–024F
        // IPA Extensions, 0250–02AF
        // Spacing Modifier Letters, 02B0–02FF
        //  - U+02C7  ˇ &#711;  Caron
        //  - U+02D8  ˘ &#728;  Breve
        //  - U+02D9  ˙ &#729;  Dot Above
        //  - U+02DA  ˚ &#730;  Ring Above
        //  - U+02DB  ˛ &#731;  Ogonek
        //  - U+02DC  ˜ &#732;  Small Tilde
        //  - U+02DD  ˝ &#733;  Double Acute Accent
        // Latin Extended Additional, 1E00–1EFF
        var extendedWordChars = /^[A-Za-z\xC0-\u02C6\u02C8-\u02D7\u02DE-\u02FF\u1E00-\u1EFF]+$/;

        var reWhitespace = /\S/;

        var wordDiff = exports. wordDiff = new _base2.default() ;
        wordDiff.equals = function (left, right) {
            return left === right || this.options.ignoreWhitespace && !reWhitespace.test(left) && !reWhitespace.test(right);
        };
        wordDiff.tokenize = function (value) {
            var tokens = value.split(/(\s+|\b)/);

            // Join the boundary splits that we do not consider to be boundaries. This is primarily the extended Latin character set.
            for (var i = 0; i < tokens.length - 1; i++) {
                // If we have an empty string in the next field and we have only word chars before and after, merge
                if (!tokens[i + 1] && tokens[i + 2] && extendedWordChars.test(tokens[i]) && extendedWordChars.test(tokens[i + 2])) {
                    tokens[i] += tokens[i + 2];
                    tokens.splice(i + 1, 2);
                    i--;
                }
            }

            return tokens;
        };

        function diffWords(oldStr, newStr, callback) {
            var options = (0, _params.generateOptions) (callback, { ignoreWhitespace: true });
            return wordDiff.diff(oldStr, newStr, options);
        }
        function diffWordsWithSpace(oldStr, newStr, callback) {
            return wordDiff.diff(oldStr, newStr, callback);
        }
        


       
    },
    /* 4 */
    function (module, exports) {

        'use strict';

        exports.__esModule = true;
        exports. generateOptions = generateOptions;
        function generateOptions(options, defaults) {
            if (typeof options === 'function') {
                defaults.callback = options;
            } else if (options) {
                for (var name in options) {
                    /* istanbul ignore else */
                    if (options.hasOwnProperty(name)) {
                        defaults[name] = options[name];
                    }
                }
            }
            return defaults;
        }
        


       
    },
    /* 5 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.lineDiff = undefined;
        exports. diffLines = diffLines;
        exports. diffTrimmedLines = diffTrimmedLines;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        
        var _params = __webpack_require__(4) ;

        
        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        var lineDiff = exports. lineDiff = new _base2.default() ;
        lineDiff.tokenize = function (value) {
            var retLines = [],
                linesAndNewlines = value.split(/(\n|\r\n)/);

            // Ignore the final empty token that occurs if the string ends with a new line
            if (!linesAndNewlines[linesAndNewlines.length - 1]) {
                linesAndNewlines.pop();
            }

            // Merge the content and line separators into single tokens
            for (var i = 0; i < linesAndNewlines.length; i++) {
                var line = linesAndNewlines[i];

                if (i % 2 && !this.options.newlineIsToken) {
                    retLines[retLines.length - 1] += line;
                } else {
                    if (this.options.ignoreWhitespace) {
                        line = line.trim();
                    }
                    retLines.push(line);
                }
            }

            return retLines;
        };

        function diffLines(oldStr, newStr, callback) {
            return lineDiff.diff(oldStr, newStr, callback);
        }
        function diffTrimmedLines(oldStr, newStr, callback) {
            var options = (0, _params.generateOptions) (callback, { ignoreWhitespace: true });
            return lineDiff.diff(oldStr, newStr, options);
        }
        


       
    },
    /* 6 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.sentenceDiff = undefined;
        exports. diffSentences = diffSentences;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        var sentenceDiff = exports. sentenceDiff = new _base2.default() ;
        sentenceDiff.tokenize = function (value) {
            return value.split(/(\S.+?[.!?])(?=\s+|$)/);
        };

        function diffSentences(oldStr, newStr, callback) {
            return sentenceDiff.diff(oldStr, newStr, callback);
        }
        


       
    },
    /* 7 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.cssDiff = undefined;
        exports. diffCss = diffCss;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        var cssDiff = exports. cssDiff = new _base2.default() ;
        cssDiff.tokenize = function (value) {
            return value.split(/([{}:;,]|\s+)/);
        };

        function diffCss(oldStr, newStr, callback) {
            return cssDiff.diff(oldStr, newStr, callback);
        }
        


       
    },
    /* 8 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports.jsonDiff = undefined;

        var _typeof = typeof Symbol === "function" && typeof Symbol.iterator === "symbol" ? function (obj) { return typeof obj; } : function (obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol ? "symbol" : typeof obj; };

        exports. diffJson = diffJson;
        exports. canonicalize = canonicalize;

        var _base = __webpack_require__(1) ;

        
        var _base2 = _interopRequireDefault(_base);

        
        var _line = __webpack_require__(5) ;

        
        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        

        var objectPrototypeToString = Object.prototype.toString;

        var jsonDiff = exports. jsonDiff = new _base2.default() ;
        // Discriminate between two lines of pretty-printed, serialized JSON where one of them has a
        // dangling comma and the other doesn't. Turns out including the dangling comma yields the nicest output:
        jsonDiff.useLongestToken = true;

        jsonDiff.tokenize = _line.lineDiff. tokenize;
        jsonDiff.castInput = function (value) {
            return typeof value === 'string' ? value : JSON.stringify(canonicalize(value), undefined, '  ');
        };
        jsonDiff.equals = function (left, right) {
            return (_base2.default. prototype.equals(left.replace(/,([\r\n])/g, '$1'), right.replace(/,([\r\n])/g, '$1'))
            );
        };

        function diffJson(oldObj, newObj, callback) {
            return jsonDiff.diff(oldObj, newObj, callback);
        }

        // This function handles the presence of circular references by bailing out when encountering an
        // object that is already on the "stack" of items being processed.
        function canonicalize(obj, stack, replacementStack) {
            stack = stack || [];
            replacementStack = replacementStack || [];

            var i = void 0 ;

            for (i = 0; i < stack.length; i += 1) {
                if (stack[i] === obj) {
                    return replacementStack[i];
                }
            }

            var canonicalizedObj = void 0 ;

            if ('[object Array]' === objectPrototypeToString.call(obj)) {
                stack.push(obj);
                canonicalizedObj = new Array(obj.length);
                replacementStack.push(canonicalizedObj);
                for (i = 0; i < obj.length; i += 1) {
                    canonicalizedObj[i] = canonicalize(obj[i], stack, replacementStack);
                }
                stack.pop();
                replacementStack.pop();
                return canonicalizedObj;
            }

            if (obj && obj.toJSON) {
                obj = obj.toJSON();
            }

            if ( (typeof obj === 'undefined' ? 'undefined' : _typeof(obj)) === 'object' && obj !== null) {
                stack.push(obj);
                canonicalizedObj = {};
                replacementStack.push(canonicalizedObj);
                var sortedKeys = [],
                    key = void 0 ;
                for (key in obj) {
                    /* istanbul ignore else */
                    if (obj.hasOwnProperty(key)) {
                        sortedKeys.push(key);
                    }
                }
                sortedKeys.sort();
                for (i = 0; i < sortedKeys.length; i += 1) {
                    key = sortedKeys[i];
                    canonicalizedObj[key] = canonicalize(obj[key], stack, replacementStack);
                }
                stack.pop();
                replacementStack.pop();
            } else {
                canonicalizedObj = obj;
            }
            return canonicalizedObj;
        }
        


       
    },
    /* 9 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports. applyPatch = applyPatch;
        exports. applyPatches = applyPatches;

        var _parse = __webpack_require__(10) ;

        var _distanceIterator = __webpack_require__(11) ;

        
        var _distanceIterator2 = _interopRequireDefault(_distanceIterator);

        function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }

        function applyPatch(source, uniDiff) {
            var options = arguments.length <= 2 || arguments[2] === undefined ? {} : arguments[2];

            if (typeof uniDiff === 'string') {
                uniDiff = (0, _parse.parsePatch) (uniDiff);
            }

            if (Array.isArray(uniDiff)) {
                if (uniDiff.length > 1) {
                    throw new Error('applyPatch only works with a single input.');
                }

                uniDiff = uniDiff[0];
            }

            // Apply the diff to the input
            var lines = source.split('\n'),
                hunks = uniDiff.hunks,
                compareLine = options.compareLine || function (lineNumber, line, operation, patchContent) {
                    //var re = /[\0-\x1F\x7F-\x9F\xAD\u0378\u0379\u037F-\u0383\u038B\u038D\u03A2\u0528-\u0530\u0557\u0558\u0560\u0588\u058B-\u058E\u0590\u05C8-\u05CF\u05EB-\u05EF\u05F5-\u0605\u061C\u061D\u06DD\u070E\u070F\u074B\u074C\u07B2-\u07BF\u07FB-\u07FF\u082E\u082F\u083F\u085C\u085D\u085F-\u089F\u08A1\u08AD-\u08E3\u08FF\u0978\u0980\u0984\u098D\u098E\u0991\u0992\u09A9\u09B1\u09B3-\u09B5\u09BA\u09BB\u09C5\u09C6\u09C9\u09CA\u09CF-\u09D6\u09D8-\u09DB\u09DE\u09E4\u09E5\u09FC-\u0A00\u0A04\u0A0B-\u0A0E\u0A11\u0A12\u0A29\u0A31\u0A34\u0A37\u0A3A\u0A3B\u0A3D\u0A43-\u0A46\u0A49\u0A4A\u0A4E-\u0A50\u0A52-\u0A58\u0A5D\u0A5F-\u0A65\u0A76-\u0A80\u0A84\u0A8E\u0A92\u0AA9\u0AB1\u0AB4\u0ABA\u0ABB\u0AC6\u0ACA\u0ACE\u0ACF\u0AD1-\u0ADF\u0AE4\u0AE5\u0AF2-\u0B00\u0B04\u0B0D\u0B0E\u0B11\u0B12\u0B29\u0B31\u0B34\u0B3A\u0B3B\u0B45\u0B46\u0B49\u0B4A\u0B4E-\u0B55\u0B58-\u0B5B\u0B5E\u0B64\u0B65\u0B78-\u0B81\u0B84\u0B8B-\u0B8D\u0B91\u0B96-\u0B98\u0B9B\u0B9D\u0BA0-\u0BA2\u0BA5-\u0BA7\u0BAB-\u0BAD\u0BBA-\u0BBD\u0BC3-\u0BC5\u0BC9\u0BCE\u0BCF\u0BD1-\u0BD6\u0BD8-\u0BE5\u0BFB-\u0C00\u0C04\u0C0D\u0C11\u0C29\u0C34\u0C3A-\u0C3C\u0C45\u0C49\u0C4E-\u0C54\u0C57\u0C5A-\u0C5F\u0C64\u0C65\u0C70-\u0C77\u0C80\u0C81\u0C84\u0C8D\u0C91\u0CA9\u0CB4\u0CBA\u0CBB\u0CC5\u0CC9\u0CCE-\u0CD4\u0CD7-\u0CDD\u0CDF\u0CE4\u0CE5\u0CF0\u0CF3-\u0D01\u0D04\u0D0D\u0D11\u0D3B\u0D3C\u0D45\u0D49\u0D4F-\u0D56\u0D58-\u0D5F\u0D64\u0D65\u0D76-\u0D78\u0D80\u0D81\u0D84\u0D97-\u0D99\u0DB2\u0DBC\u0DBE\u0DBF\u0DC7-\u0DC9\u0DCB-\u0DCE\u0DD5\u0DD7\u0DE0-\u0DF1\u0DF5-\u0E00\u0E3B-\u0E3E\u0E5C-\u0E80\u0E83\u0E85\u0E86\u0E89\u0E8B\u0E8C\u0E8E-\u0E93\u0E98\u0EA0\u0EA4\u0EA6\u0EA8\u0EA9\u0EAC\u0EBA\u0EBE\u0EBF\u0EC5\u0EC7\u0ECE\u0ECF\u0EDA\u0EDB\u0EE0-\u0EFF\u0F48\u0F6D-\u0F70\u0F98\u0FBD\u0FCD\u0FDB-\u0FFF\u10C6\u10C8-\u10CC\u10CE\u10CF\u1249\u124E\u124F\u1257\u1259\u125E\u125F\u1289\u128E\u128F\u12B1\u12B6\u12B7\u12BF\u12C1\u12C6\u12C7\u12D7\u1311\u1316\u1317\u135B\u135C\u137D-\u137F\u139A-\u139F\u13F5-\u13FF\u169D-\u169F\u16F1-\u16FF\u170D\u1715-\u171F\u1737-\u173F\u1754-\u175F\u176D\u1771\u1774-\u177F\u17DE\u17DF\u17EA-\u17EF\u17FA-\u17FF\u180F\u181A-\u181F\u1878-\u187F\u18AB-\u18AF\u18F6-\u18FF\u191D-\u191F\u192C-\u192F\u193C-\u193F\u1941-\u1943\u196E\u196F\u1975-\u197F\u19AC-\u19AF\u19CA-\u19CF\u19DB-\u19DD\u1A1C\u1A1D\u1A5F\u1A7D\u1A7E\u1A8A-\u1A8F\u1A9A-\u1A9F\u1AAE-\u1AFF\u1B4C-\u1B4F\u1B7D-\u1B7F\u1BF4-\u1BFB\u1C38-\u1C3A\u1C4A-\u1C4C\u1C80-\u1CBF\u1CC8-\u1CCF\u1CF7-\u1CFF\u1DE7-\u1DFB\u1F16\u1F17\u1F1E\u1F1F\u1F46\u1F47\u1F4E\u1F4F\u1F58\u1F5A\u1F5C\u1F5E\u1F7E\u1F7F\u1FB5\u1FC5\u1FD4\u1FD5\u1FDC\u1FF0\u1FF1\u1FF5\u1FFF\u200B-\u200F\u202A-\u202E\u2060-\u206F\u2072\u2073\u208F\u209D-\u209F\u20BB-\u20CF\u20F1-\u20FF\u218A-\u218F\u23F4-\u23FF\u2427-\u243F\u244B-\u245F\u2700\u2B4D-\u2B4F\u2B5A-\u2BFF\u2C2F\u2C5F\u2CF4-\u2CF8\u2D26\u2D28-\u2D2C\u2D2E\u2D2F\u2D68-\u2D6E\u2D71-\u2D7E\u2D97-\u2D9F\u2DA7\u2DAF\u2DB7\u2DBF\u2DC7\u2DCF\u2DD7\u2DDF\u2E3C-\u2E7F\u2E9A\u2EF4-\u2EFF\u2FD6-\u2FEF\u2FFC-\u2FFF\u3040\u3097\u3098\u3100-\u3104\u312E-\u3130\u318F\u31BB-\u31BF\u31E4-\u31EF\u321F\u32FF\u4DB6-\u4DBF\u9FCD-\u9FFF\uA48D-\uA48F\uA4C7-\uA4CF\uA62C-\uA63F\uA698-\uA69E\uA6F8-\uA6FF\uA78F\uA794-\uA79F\uA7AB-\uA7F7\uA82C-\uA82F\uA83A-\uA83F\uA878-\uA87F\uA8C5-\uA8CD\uA8DA-\uA8DF\uA8FC-\uA8FF\uA954-\uA95E\uA97D-\uA97F\uA9CE\uA9DA-\uA9DD\uA9E0-\uA9FF\uAA37-\uAA3F\uAA4E\uAA4F\uAA5A\uAA5B\uAA7C-\uAA7F\uAAC3-\uAADA\uAAF7-\uAB00\uAB07\uAB08\uAB0F\uAB10\uAB17-\uAB1F\uAB27\uAB2F-\uABBF\uABEE\uABEF\uABFA-\uABFF\uD7A4-\uD7AF\uD7C7-\uD7CA\uD7FC-\uF8FF\uFA6E\uFA6F\uFADA-\uFAFF\uFB07-\uFB12\uFB18-\uFB1C\uFB37\uFB3D\uFB3F\uFB42\uFB45\uFBC2-\uFBD2\uFD40-\uFD4F\uFD90\uFD91\uFDC8-\uFDEF\uFDFE\uFDFF\uFE1A-\uFE1F\uFE27-\uFE2F\uFE53\uFE67\uFE6C-\uFE6F\uFE75\uFEFD-\uFF00\uFFBF-\uFFC1\uFFC8\uFFC9\uFFD0\uFFD1\uFFD8\uFFD9\uFFDD-\uFFDF\uFFE7\uFFEF-\uFFFB\uFFFE\uFFFF]/g;

                    //return (line.replace(re, "") === patchContent.replace(re, ""));
                    
                    return (line === patchContent);
                },
                errorCount = 0,
                fuzzFactor = options.fuzzFactor || 0,
                minLine = 0,
                offset = 0,
                removeEOFNL = void 0 ,
                addEOFNL = void 0 ;

            /**
             * Checks if the hunk exactly fits on the provided location
             */
            function hunkFits(hunk, toPos) {
                for (var j = 0; j < hunk.lines.length; j++) {
                    var line = hunk.lines[j],
                        operation = line[0],
                        content = line.substr(1);

                    if (operation === ' ' || operation === '-') {
                        // Context sanity check
                        if (!compareLine(toPos + 1, lines[toPos], operation, content)) {
                            errorCount++;

                            if (errorCount > fuzzFactor) {
                                return false;
                            }
                        }
                        toPos++;
                    }
                }

                return true;
            }

            // Search best fit offsets for each hunk based on the previous ones
            for (var i = 0; i < hunks.length; i++) {
                var hunk = hunks[i],
                    maxLine = lines.length - hunk.oldLines,
                    localOffset = 0,
                    toPos = offset + hunk.oldStart - 1;

                var iterator = (0, _distanceIterator2.default) (toPos, minLine, maxLine);

                for (; localOffset !== undefined; localOffset = iterator()) {
                    if (hunkFits(hunk, toPos + localOffset)) {
                        hunk.offset = offset += localOffset;
                        break;
                    }
                }

                if (localOffset === undefined) {
                    return false;
                }

                // Set lower text limit to end of the current hunk, so next ones don't try
                // to fit over already patched text
                minLine = hunk.offset + hunk.oldStart + hunk.oldLines;
            }

            // Apply patch hunks
            for (var _i = 0; _i < hunks.length; _i++) {
                var _hunk = hunks[_i],
                    _toPos = _hunk.offset + _hunk.newStart - 1;

                for (var j = 0; j < _hunk.lines.length; j++) {
                    var line = _hunk.lines[j],
                        operation = line[0],
                        content = line.substr(1);

                    if (operation === ' ') {
                        _toPos++;
                    } else if (operation === '-') {
                        lines.splice(_toPos, 1);
                        /* istanbul ignore else */
                    } else if (operation === '+') {
                        lines.splice(_toPos, 0, content);
                        _toPos++;
                    } else if (operation === '\\') {
                        var previousOperation = _hunk.lines[j - 1] ? _hunk.lines[j - 1][0] : null;
                        if (previousOperation === '+') {
                            removeEOFNL = true;
                        } else if (previousOperation === '-') {
                            addEOFNL = true;
                        }
                    }
                }
            }

            // Handle EOFNL insertion/removal
            if (removeEOFNL) {
                while (!lines[lines.length - 1]) {
                    lines.pop();
                }
            } else if (addEOFNL) {
                lines.push('');
            }
            return lines.join('\n');
        }

        // Wrapper that supports multiple file patches via callbacks.
        function applyPatches(uniDiff, options) {
            if (typeof uniDiff === 'string') {
                uniDiff = (0, _parse.parsePatch) (uniDiff);
            }

            var currentIndex = 0;
            function processIndex() {
                var index = uniDiff[currentIndex++];
                if (!index) {
                    return options.complete();
                }

                options.loadFile(index, function (err, data) {
                    if (err) {
                        return options.complete(err);
                    }

                    var updatedContent = applyPatch(data, index, options);
                    options.patched(index, updatedContent);

                    setTimeout(processIndex, 0);
                });
            }
            processIndex();
        }
        


       
    },
    /* 10 */
    function (module, exports) {

        'use strict';

        exports.__esModule = true;
        exports. parsePatch = parsePatch;
        function parsePatch(uniDiff) {
            var options = arguments.length <= 1 || arguments[1] === undefined ? {} : arguments[1];

            var diffstr = uniDiff.split('\n'),
                list = [],
                i = 0;

            function parseIndex() {
                var index = {};
                list.push(index);

                // Parse diff metadata
                while (i < diffstr.length) {
                    var line = diffstr[i];

                    // File header found, end parsing diff metadata
                    if (/^(\-\-\-|\+\+\+|@@)\s/.test(line)) {
                        break;
                    }

                    // Diff index
                    var header = /^(?:Index:|diff(?: -r \w+)+)\s+(.+?)\s*$/.exec(line);
                    if (header) {
                        index.index = header[1];
                    }

                    i++;
                }

                // Parse file headers if they are defined. Unified diff requires them, but
                // there's no technical issues to have an isolated hunk without file header
                parseFileHeader(index);
                parseFileHeader(index);

                // Parse hunks
                index.hunks = [];

                while (i < diffstr.length) {
                    var _line = diffstr[i];

                    if (/^(Index:|diff|\-\-\-|\+\+\+)\s/.test(_line)) {
                        break;
                    } else if (/^@@/.test(_line)) {
                        index.hunks.push(parseHunk());
                    } else if (_line && options.strict) {
                        // Ignore unexpected content unless in strict mode
                        throw new Error('Unknown line ' + (i + 1) + ' ' + JSON.stringify(_line));
                    } else {
                        i++;
                    }
                }
            }

            // Parses the --- and +++ headers, if none are found, no lines
            // are consumed.
            function parseFileHeader(index) {
                var fileHeader = /^(\-\-\-|\+\+\+)\s+(\S*)\s?(.*?)\s*$/.exec(diffstr[i]);
                if (fileHeader) {
                    var keyPrefix = fileHeader[1] === '---' ? 'old' : 'new';
                    index[keyPrefix + 'FileName'] = fileHeader[2];
                    index[keyPrefix + 'Header'] = fileHeader[3];

                    i++;
                }
            }

            // Parses a hunk
            // This assumes that we are at the start of a hunk.
            function parseHunk() {
                var chunkHeaderIndex = i,
                    chunkHeaderLine = diffstr[i++],
                    chunkHeader = chunkHeaderLine.split(/@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@/);

                var hunk = {
                    oldStart: +chunkHeader[1],
                    oldLines: +chunkHeader[2] || 1,
                    newStart: +chunkHeader[3],
                    newLines: +chunkHeader[4] || 1,
                    lines: []
                };

                var addCount = 0,
                    removeCount = 0;
                for (; i < diffstr.length; i++) {
                    var operation = diffstr[i][0];

                    if (operation === '+' || operation === '-' || operation === ' ' || operation === '\\') {
                        hunk.lines.push(diffstr[i]);

                        if (operation === '+') {
                            addCount++;
                        } else if (operation === '-') {
                            removeCount++;
                        } else if (operation === ' ') {
                            addCount++;
                            removeCount++;
                        }
                    } else {
                        break;
                    }
                }

                // Handle the empty block count case
                if (!addCount && hunk.newLines === 1) {
                    hunk.newLines = 0;
                }
                if (!removeCount && hunk.oldLines === 1) {
                    hunk.oldLines = 0;
                }

                // Perform optional sanity checking
                if (options.strict) {
                    if (addCount !== hunk.newLines) {
                        throw new Error('Added line count did not match for hunk at line ' + (chunkHeaderIndex + 1));
                    }
                    if (removeCount !== hunk.oldLines) {
                        throw new Error('Removed line count did not match for hunk at line ' + (chunkHeaderIndex + 1));
                    }
                }

                return hunk;
            }

            while (i < diffstr.length) {
                parseIndex();
            }

            return list;
        }
        


       
    },
    /* 11 */
    function (module, exports) {

        "use strict";

        exports.__esModule = true;

        exports.default = function (start, minLine, maxLine) {
            var wantForward = true,
                backwardExhausted = false,
                forwardExhausted = false,
                localOffset = 1;

            return function iterator() {
                if (wantForward && !forwardExhausted) {
                    if (backwardExhausted) {
                        localOffset++;
                    } else {
                        wantForward = false;
                    }

                    // Check if trying to fit beyond text length, and if not, check it fits
                    // after offset location (or desired location on first iteration)
                    if (start + localOffset <= maxLine) {
                        return localOffset;
                    }

                    forwardExhausted = true;
                }

                if (!backwardExhausted) {
                    if (!forwardExhausted) {
                        wantForward = true;
                    }

                    // Check if trying to fit before text beginning, and if not, check it fits
                    // before offset location
                    if (minLine <= start - localOffset) {
                        return -localOffset++;
                    }

                    backwardExhausted = true;
                    return iterator();
                }

                // We tried to fit hunk before text beginning and beyond text lenght, then
                // hunk can't fit on the text. Return undefined
            };
        };
        


       
    },
    /* 12 */
    function (module, exports, __webpack_require__) {

        'use strict';

        exports.__esModule = true;
        exports. structuredPatch = structuredPatch;
        exports. createTwoFilesPatch = createTwoFilesPatch;
        exports. createPatch = createPatch;

        var _line = __webpack_require__(5) ;

        
        function _toConsumableArray(arr) { if (Array.isArray(arr)) { for (var i = 0, arr2 = Array(arr.length) ; i < arr.length; i++) { arr2[i] = arr[i]; } return arr2; } else { return Array.from(arr); } }

        function structuredPatch(oldFileName, newFileName, oldStr, newStr, oldHeader, newHeader, options) {
            if (!options) {
                options = { context: 4 };
            }

            var diff = (0, _line.diffLines) (oldStr, newStr);
            diff.push({ value: '', lines: [] }); // Append an empty value to make cleanup easier

            function contextLines(lines) {
                return lines.map(function (entry) {
                    return ' ' + entry;
                });
            }

            var hunks = [];
            var oldRangeStart = 0,
                newRangeStart = 0,
                curRange = [],
                oldLine = 1,
                newLine = 1;
            
            var _loop = function _loop( i) {
                var current = diff[i],
                    lines = current.lines || current.value.replace(/\n$/, '').split('\n');
                current.lines = lines;

                if (current.added || current.removed) {
                    
                    var _curRange;

                    
                    // If we have previous context, start with that
                    if (!oldRangeStart) {
                        var prev = diff[i - 1];
                        oldRangeStart = oldLine;
                        newRangeStart = newLine;

                        if (prev) {
                            curRange = options.context > 0 ? contextLines(prev.lines.slice(-options.context)) : [];
                            oldRangeStart -= curRange.length;
                            newRangeStart -= curRange.length;
                        }
                    }

                    // Output our changes
                    (_curRange = curRange).push. apply ( _curRange , _toConsumableArray( lines.map(function (entry) {
                        return (current.added ? '+' : '-') + entry;
                    })));

                    // Track the updated file position
                    if (current.added) {
                        newLine += lines.length;
                    } else {
                        oldLine += lines.length;
                    }
                } else {
                    // Identical context lines. Track line changes
                    if (oldRangeStart) {
                        // Close out any changes that have been output (or join overlapping)
                        if (lines.length <= options.context * 2 && i < diff.length - 2) {
                            
                            var _curRange2;

                            
                            // Overlapping
                            (_curRange2 = curRange).push. apply ( _curRange2 , _toConsumableArray( contextLines(lines)));
                        } else {
                            
                            var _curRange3;

                            
                            // end the range and output
                            var contextSize = Math.min(lines.length, options.context);
                            (_curRange3 = curRange).push. apply ( _curRange3 , _toConsumableArray( contextLines(lines.slice(0, contextSize))));

                            var hunk = {
                                oldStart: oldRangeStart,
                                oldLines: oldLine - oldRangeStart + contextSize,
                                newStart: newRangeStart,
                                newLines: newLine - newRangeStart + contextSize,
                                lines: curRange
                            };
                            if (i >= diff.length - 2 && lines.length <= options.context) {
                                // EOF is inside this hunk
                                var oldEOFNewline = /\n$/.test(oldStr);
                                var newEOFNewline = /\n$/.test(newStr);
                                if (lines.length == 0 && !oldEOFNewline) {
                                    // special case: old has no eol and no trailing context; no-nl can end up before adds
                                    curRange.splice(hunk.oldLines, 0, '\\ No newline at end of file');
                                } else if (!oldEOFNewline || !newEOFNewline) {
                                    curRange.push('\\ No newline at end of file');
                                }
                            }
                            hunks.push(hunk);

                            oldRangeStart = 0;
                            newRangeStart = 0;
                            curRange = [];
                        }
                    }
                    oldLine += lines.length;
                    newLine += lines.length;
                }
            };

            for (var i = 0; i < diff.length; i++) {
                
                _loop( i);
            }

            return {
                oldFileName: oldFileName, newFileName: newFileName,
                oldHeader: oldHeader, newHeader: newHeader,
                hunks: hunks
            };
        }

        function createTwoFilesPatch(oldFileName, newFileName, oldStr, newStr, oldHeader, newHeader, options) {
            var diff = structuredPatch(oldFileName, newFileName, oldStr, newStr, oldHeader, newHeader, options);

            var ret = [];
            if (oldFileName == newFileName) {
                ret.push('Index: ' + oldFileName);
            }
            ret.push('===================================================================');
            ret.push('--- ' + diff.oldFileName + (typeof diff.oldHeader === 'undefined' ? '' : '\t' + diff.oldHeader));
            ret.push('+++ ' + diff.newFileName + (typeof diff.newHeader === 'undefined' ? '' : '\t' + diff.newHeader));

            for (var i = 0; i < diff.hunks.length; i++) {
                var hunk = diff.hunks[i];
                ret.push('@@ -' + hunk.oldStart + ',' + hunk.oldLines + ' +' + hunk.newStart + ',' + hunk.newLines + ' @@');
                ret.push.apply(ret, hunk.lines);
            }

            return ret.join('\n') + '\n';
        }

        function createPatch(fileName, oldStr, newStr, oldHeader, newHeader, options) {
            return createTwoFilesPatch(fileName, fileName, oldStr, newStr, oldHeader, newHeader, options);
        }
        


       
    },
    /* 13 */
    function (module, exports) {

        "use strict";

        exports.__esModule = true;
        exports. convertChangesToDMP = convertChangesToDMP;
        // See: http://code.google.com/p/google-diff-match-patch/wiki/API
        function convertChangesToDMP(changes) {
            var ret = [],
                change = void 0 ,
                operation = void 0 ;
            for (var i = 0; i < changes.length; i++) {
                change = changes[i];
                if (change.added) {
                    operation = 1;
                } else if (change.removed) {
                    operation = -1;
                } else {
                    operation = 0;
                }

                ret.push([operation, change.value]);
            }
            return ret;
        }
        


       
    },
    /* 14 */
    function (module, exports) {

        'use strict';

        exports.__esModule = true;
        exports. convertChangesToXML = convertChangesToXML;
        function convertChangesToXML(changes) {
            var ret = [];
            for (var i = 0; i < changes.length; i++) {
                var change = changes[i];
                if (change.added) {
                    ret.push('<ins>');
                } else if (change.removed) {
                    ret.push('<del>');
                }

                ret.push(escapeHTML(change.value));

                if (change.added) {
                    ret.push('</ins>');
                } else if (change.removed) {
                    ret.push('</del>');
                }
            }
            return ret.join('');
        }

        function escapeHTML(s) {
            var n = s;
            n = n.replace(/&/g, '&amp;');
            n = n.replace(/</g, '&lt;');
            n = n.replace(/>/g, '&gt;');
            n = n.replace(/"/g, '&quot;');

            return n;
        }
        


       
    }
    ])
});
;