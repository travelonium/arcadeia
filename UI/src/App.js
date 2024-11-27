import React, { Component } from 'react';
import NavMenu from './components/NavMenu';
import Library from './components/Library';
import Settings from './components/Settings';
import { setTheme } from './features/ui/slice';
import { Route, Routes } from 'react-router';
import { connect } from "react-redux";

class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        // initialize the theme setting it to dark/light mode
        this.onSelectMode();
    }

    componentDidMount() {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => this.onSelectMode(e.matches ? 'dark' : 'light'));
    }

    componentWillUnmount() {
        window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', e => this.onSelectMode(this, e.matches ? 'dark' : 'light'));
    }

    onSelectMode(mode) {
        if (!mode) mode = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        this.props.dispatch(setTheme(mode));
    }

    render() {
        return (
            <div className="d-flex flex-column align-content-stretch h-100">
                <NavMenu library={this.props.library} />
                <Routes>
                    <Route path="/settings/*" element={<Settings />} />
                    <Route exact path='/*' element={<Library ref={this.library} forwardedRef={this.library} />} />
                </Routes>
            </div>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        theme: state.ui.theme,
    }
});

export default connect(mapStateToProps, null, null)(App);

