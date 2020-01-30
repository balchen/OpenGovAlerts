import React, { Component } from 'react';

export class AddObserver extends Component {
    displayName = AddObserver.name;

    constructor(props) {
        super(props);

        this.state = {
            name: ''
        };

        this.save = this.save.bind(this);
    }

    save() {
        this.props.onSave({ name: this.state.name });
    }

    render() {
        return <div><div><label>Navn:</label><input type="text" value={this.state.name} onChange={e => this.setState({ name: e.target.value })} /></div><button onClick={this.save}>Lagre</button></div>;
    }
}
