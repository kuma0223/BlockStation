//ルーター
window.Router = new function() {
    //property
    this.state = {
        key: "root"
    }
    this.pages = [
        //key, path は必須
        { key:"root", path:"/" },
        { key:"not-found", path:"/404.html" },
    ]

    //Method
    this.initialize = initialize
    this.navigate = navigate
    this.navigateByKey = navigateByKey

    //script
    var me = this

    window.onpopstate = function(){
        initialize()
    }

    function initialize(){
        var pg = getPage(window.location.pathname)
        if(pg != null){
            setState(pg)
        } else if(getPageByKey("not-found")){
            window.location.href = getPageByKey("not-found").path
        }
    }

    function getPageByKey(key){
        for(let i=0; i<me.pages.length; i++){
            if(me.pages[i].key == key)
                return me.pages[i];
        }
        return null;
    }

    function getPage(path){
        for(let i=0; i<me.pages.length; i++){
            if(me.pages[i].path == path)
                return me.pages[i];
        }
        return null
    }

    function navigate(path){
        var pg = getPage(path)
        if(pg==null){
            //outside
            window.location.href = path
            return
        }
        if(me.state.key != pg.key){
            setState(pg)
            window.history.pushState(null, null, pg.path)
        }
    }

    function navigateByKey(key){
        if(key == me.state.key) return
        var pg = getPageByKey(key)
        if(pg != null){
            setState(pg)
            window.history.pushState(null, null, pg.path)
        }
    }

    function setState(page){
        Object.keys(page).forEach(x => {
            me.state[x] = page[x]
        });
    }
}

export const Router = window.Router