// d2wrapper.go
package main

/*
#include <stdlib.h>
*/
import "C"

import (
	"context"
	"fmt"
	"os"
	"unsafe"

	"cdr.dev/slog"

	"oss.terrastruct.com/d2/d2graph"
	"oss.terrastruct.com/d2/d2layouts/d2dagrelayout"
	"oss.terrastruct.com/d2/d2lib"
	"oss.terrastruct.com/d2/d2renderers/d2svg"
	"oss.terrastruct.com/d2/d2themes/d2themescatalog"
	"oss.terrastruct.com/d2/lib/log"
	"oss.terrastruct.com/d2/lib/textmeasure"
	"oss.terrastruct.com/util-go/go2"
)

var themeMap = map[int]int64{
	0: d2themescatalog.GrapeSoda.ID,
	1: d2themescatalog.Aubergine.ID,
	2: d2themescatalog.ButteredToast.ID,
	// Add more theme mappings as needed
}

var paddingMap = map[int]int64{
	0: 0,
	1: 5,
	2: 10,
	3: 20,
	// Add more padding mappings as needed
}

//export RenderDiagram
func RenderDiagram(script *C.char, themeID int64, pad int64, errorPtr **C.char) *C.char {
	goScript := C.GoString(script)

	ruler, _ := textmeasure.NewRuler()
	logger := slog.Logger{}
	layoutResolver := func(engine string) (d2graph.LayoutGraph, error) {
		return d2dagrelayout.DefaultLayout, nil
	}

	renderOpts := &d2svg.RenderOpts{
		Pad:     go2.Pointer(paddingMap[int(pad)]),
		ThemeID: go2.Pointer(themeMap[int(themeID)]),
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
