// d2wrapper.go
package main

/*
#include <stdlib.h>
*/
import "C"

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	slog "log/slog"
	"os"
	"runtime"
	"unsafe"

	"oss.terrastruct.com/d2/d2graph"
	"oss.terrastruct.com/d2/d2layouts/d2dagrelayout"
	"oss.terrastruct.com/d2/d2layouts/d2elklayout"
	"oss.terrastruct.com/d2/d2lib"
	"oss.terrastruct.com/d2/d2renderers/d2svg"
	"oss.terrastruct.com/d2/lib/log"
	"oss.terrastruct.com/d2/lib/textmeasure"
)

// RenderOptionsJSON represents the JSON structure for render options from C#
type RenderOptionsJSON struct {
	Layout         *string  `json:"layout"`
	ThemeID        *int64   `json:"themeId"`
	DarkThemeID    *int64   `json:"darkThemeId"`
	Sketch         *bool    `json:"sketch"`
	Pad            *int64   `json:"pad"`
	Scale          *float64 `json:"scale"`
	Center         *bool    `json:"center"`
	Target         *string  `json:"target"`
	AnimateInterval *int64  `json:"animateInterval"`
	ForceAppendix  *bool    `json:"forceAppendix"`
}

//export RenderDiagram
func RenderDiagram(script *C.char, optionsJSON *C.char, errorPtr **C.char) *C.char {
	// Recover from panics to prevent crashing the host process
	defer func() {
		if r := recover(); r != nil {
			*errorPtr = C.CString(fmt.Sprintf("Panic during rendering: %v", r))
		}
	}()

	// Lock this goroutine to the current OS thread
	// This ensures all work happens on the .NET-provided thread with large stack
	runtime.LockOSThread()
	defer runtime.UnlockOSThread()

	// Force Go to use only 1 OS thread to prevent creating threads with small stacks
	oldMaxProcs := runtime.GOMAXPROCS(1)
	defer runtime.GOMAXPROCS(oldMaxProcs)

	goScript := C.GoString(script)
	goOptionsJSON := C.GoString(optionsJSON)

	// Parse options from JSON
	var opts RenderOptionsJSON
	if goOptionsJSON != "" && goOptionsJSON != "null" {
		if err := json.Unmarshal([]byte(goOptionsJSON), &opts); err != nil {
			*errorPtr = C.CString(fmt.Sprintf("Options parsing error: %v", err))
			return nil
		}
	}

	ruler, err := textmeasure.NewRuler()
	if err != nil {
		*errorPtr = C.CString(fmt.Sprintf("Text measurement initialization error: %v", err))
		return nil
	}

	// Create a logger that discards output (quiet mode for library use)
	logger := slog.New(slog.NewTextHandler(io.Discard, nil))

	// Determine layout engine
	layoutResolver := func(engine string) (d2graph.LayoutGraph, error) {
		if opts.Layout != nil && *opts.Layout == "elk" {
			return d2elklayout.DefaultLayout, nil
		}
		return d2dagrelayout.DefaultLayout, nil
	}

	// Configure render options
	renderOpts := &d2svg.RenderOpts{}

	if opts.ThemeID != nil {
		renderOpts.ThemeID = opts.ThemeID
	}
	if opts.DarkThemeID != nil {
		renderOpts.DarkThemeID = opts.DarkThemeID
	}
	if opts.Pad != nil {
		renderOpts.Pad = opts.Pad
	}
	if opts.Center != nil {
		renderOpts.Center = opts.Center
	}
	if opts.Sketch != nil {
		renderOpts.Sketch = opts.Sketch
	}
	if opts.Scale != nil {
		renderOpts.Scale = opts.Scale
	}

	compileOpts := &d2lib.CompileOptions{
		LayoutResolver: layoutResolver,
		Ruler:          ruler,
	}

	ctx := context.Background()
	ctx = log.With(ctx, logger)

	diagram, _, err := d2lib.Compile(ctx, goScript, compileOpts, renderOpts)
	if err != nil {
		*errorPtr = C.CString(fmt.Sprintf("Compilation error: %v", err))
		return nil
	}

	out, err := d2svg.Render(diagram, renderOpts)
	if err != nil {
		*errorPtr = C.CString(fmt.Sprintf("Rendering error: %v", err))
		return nil
	}

	return C.CString(string(out))
}

//export FreeDiagram
func FreeDiagram(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}

func SaveDiagramToFile(svg, filename string) error {
	return os.WriteFile(filename, []byte(svg), 0600)
}

func main() {}
