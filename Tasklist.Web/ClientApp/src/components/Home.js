import React, { Component } from 'react';
import DataTable from 'react-data-table-component';
import { w3cwebsocket as WebSocket } from 'websocket';

const columns = [
  {
    name: 'id',
    selector: 'id',
    omit: true,
  },
  {
    name: 'Name',
    selector: 'name',
    sortable: true,
  },
  {
    name: 'CPU',
    selector: 'cpuLoad',
    sortable: true,
    right: true,
  },
];

export class Home extends Component {
  constructor(props) {
    super(props);

    this.state = {
      loading: true,
    };
  }

  taskListSocket = new WebSocket('wss://localhost:44336/ws-cpu-all');
  lowSysSocket = new WebSocket('wss://localhost:44336/ws-low-sys');

  updateItems(items) {
    let i = 0;
    items.forEach((element) => {
      element.id = i++;
    });
    this.setState({
      isLoaded: true,
      items: items,
    });
  }
  componentDidMount() {
    fetch('https://localhost:44336/tasklist')
      .then((res) => res.json())
      .then(
        (result) => {
          this.updateItems(result);
        },
        // Примечание: важно обрабатывать ошибки именно здесь, а не в блоке catch(),
        // чтобы не перехватывать исключения из ошибок в самих компонентах.
        (error) => {
          this.setState({
            isLoaded: true,
            error,
          });
        }
      );

    this.taskListSocket.onmessage = (evt) => {
      // listen to data sent from the websocket server
      var data = JSON.parse(evt.data);
      this.updateItems(data);
    };
    this.lowSysSocket.onmessage = (evt) => {
      // listen to data sent from the websocket server
      var flags = JSON.parse(evt.data);
      this.setState({
        highCpu: flags.highCpu,
        lowMemory: flags.lowMemory,
      });
    };
  }

  render() {
    const { error, isLoaded, items, highCpu, lowMemory } = this.state;

    const errorStyle = {
      fontColor: 'red',
      color: 'red',
    };
    if (error) {
      return <div>Error: {error.message}</div>;
    } else if (!isLoaded) {
      return <div>Loading...</div>;
    } else {
      return (
        <div>
          <div style={{ paddingLeft: 18 }}>
            <h4>
              CPU level is&nbsp;
              <span className={highCpu ? 'warning' : 'ok'}>
                {highCpu ? 'high' : 'ok'}
              </span>
            </h4>
          </div>
          <div style={{ paddingLeft: 18 }}>
            <h4>
              Memory level is&nbsp;
              <span className={lowMemory ? 'warning' : 'ok'}>
                {lowMemory ? 'low' : 'ok'}
              </span>
            </h4>
          </div>
          <hr />
          <DataTable
            title="Task manager"
            columns={columns}
            data={items}
            keyField="id"
            defaultSortAsc={false}
            defaultSortField="cpuLoad"
          />
        </div>
      );
    }
  }
}
