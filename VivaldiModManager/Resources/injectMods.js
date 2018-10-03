(function () {
	var jsToInject = []
	
	function injectCSS(file) {
		var link = document.createElement('link');
		link.rel = 'stylesheet';
		link.href = file;
		document.getElementsByTagName('head')[0].appendChild(link);
	}
	
	function injectJS(file) {
		var script = document.createElement('script');
		script.type = 'text/javascript';
		script.src = file;
		document.getElementsByTagName('body')[0].appendChild(script);
	}
	
	function injectMod(mod) {
		var modPath = mod.fullPath.replace('/crxfs/', '');
		console.log('Injecting mod "' + modPath)
		if (mod.isFile) {
			mod.name.toLowerCase().endsWith('.js') && jsToInject.push(modPath)
			mod.name.toLowerCase().endsWith('.css') && injectCSS(modPath)
		}
	}
	
	function readDirectoryAndInjectMods(directory) {
		if (directory.isDirectory) {
			directory.createReader().readEntries(e => {
				e.forEach(mod => {
					if (mod.isDirectory) {
						readDirectoryAndInjectMods(mod)
					} else injectMod(mod)
				})
			})
		}
	}
	
	function injectMods() {	
		chrome.runtime.getPackageDirectoryEntry(e => {
			e.createReader().readEntries(e => {
				e.forEach(e => {
					e.isDirectory && 'user_mods' === e.name && readDirectoryAndInjectMods(e)
				})
			})
		})
	}
	
	injectMods()
	setTimeout(function wait() {
		const browser = document.getElementById('browser');
		if (browser) {
			setTimeout(() => jsToInject.forEach(js => injectJS(js)), 100)
		}
		else {
			setTimeout(wait, 300);
		}
	}, 300);
})()