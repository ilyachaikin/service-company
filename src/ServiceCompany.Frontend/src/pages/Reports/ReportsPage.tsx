import React, { useState, useCallback } from 'react';
import {
  Card, Row, Col, Typography, DatePicker, Button, Statistic, Table,
  Select, Space, Tag, Spin, Progress, message,
} from 'antd';
import {
  BarChartOutlined, TeamOutlined, ToolOutlined,
  EnvironmentOutlined, ClockCircleOutlined, SearchOutlined,
} from '@ant-design/icons';
import api from '../../api/client';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const priorityLabels: Record<number, { label: string; color: string }> = {
  0: { label: 'Критический', color: 'red' },
  1: { label: 'Высокий',     color: 'volcano' },
  2: { label: 'Обычный',     color: 'gold' },
  3: { label: 'Низкий',      color: 'green' },
};

const ReportsPage: React.FC = () => {
  const defaultRange: [Dayjs, Dayjs] = [dayjs().subtract(30, 'day'), dayjs()];
  const [range, setRange] = useState<[Dayjs, Dayjs]>(defaultRange);
  const [clients, setClients] = useState<any[]>([]);
  const [selectedClient, setSelectedClient] = useState<string | undefined>(undefined);
  const [clientsLoaded, setClientsLoaded] = useState(false);

  const [slaLoading, setSlaLoading]             = useState(false);
  const [workloadLoading, setWorkloadLoading]   = useState(false);
  const [equipmentLoading, setEquipmentLoading] = useState(false);
  const [objectsLoading, setObjectsLoading]     = useState(false);
  const [priorityLoading, setPriorityLoading]   = useState(false);

  const [slaData, setSlaData]           = useState<any>(null);
  const [workloadData, setWorkloadData] = useState<any[]>([]);
  const [equipmentData, setEquipmentData] = useState<any[]>([]);
  const [objectsData, setObjectsData]   = useState<any[]>([]);
  const [priorityData, setPriorityData] = useState<any[]>([]);
  const [loaded, setLoaded]             = useState(false);

  const loadClients = async () => {
    if (clientsLoaded) return;
    try {
      const res = await api.get('/Clients', { params: { page: 1, pageSize: 500 } });
      setClients(res.data.items ?? res.data);
      setClientsLoaded(true);
    } catch {  }
  };

  const fetchAll = useCallback(async () => {
    const from = range[0].startOf('day').toISOString();
    const to   = range[1].endOf('day').toISOString();

    setSlaLoading(true);
    setWorkloadLoading(true);
    setEquipmentLoading(true);
    setObjectsLoading(true);
    setPriorityLoading(true);
    setLoaded(true);

    const run = async <T,>(
      setter: (v: T) => void,
      loadSetter: (v: boolean) => void,
      url: string,
      extra?: Record<string, any>,
    ) => {
      try {
        const res = await api.get(url, { params: { from, to, ...extra } });
        setter(res.data);
      } catch {
        message.error(`Не удалось загрузить: ${url}`);
      } finally {
        loadSetter(false);
      }
    };

    await Promise.all([
      run<any>(setSlaData, setSlaLoading,           '/reports/sla',                    selectedClient ? { clientId: selectedClient } : {}),
      run<any[]>(setWorkloadData, setWorkloadLoading, '/reports/engineer-workload',     {}),
      run<any[]>(setEquipmentData, setEquipmentLoading, '/reports/problematic-equipment', { top: 10 }),
      run<any[]>(setObjectsData, setObjectsLoading,  '/reports/tickets-by-object',     selectedClient ? { clientId: selectedClient } : {}),
      run<any[]>(setPriorityData, setPriorityLoading, '/reports/resolution-by-priority', {}),
    ]);
  }, [range, selectedClient]);

  const workloadColumns = [
    { title: 'Инженер',               dataIndex: 'engineerName',       key: 'name' },
    { title: 'Активных заявок',       dataIndex: 'activeTickets',      key: 'active',  sorter: (a: any, b: any) => a.activeTickets - b.activeTickets },
    { title: 'Закрыто за период',     dataIndex: 'closedThisPeriod',   key: 'closed',  sorter: (a: any, b: any) => a.closedThisPeriod - b.closedThisPeriod },
    {
      title: 'Среднее время решения',
      dataIndex: 'avgResolutionHours',
      key: 'avg',
      sorter: (a: any, b: any) => a.avgResolutionHours - b.avgResolutionHours,
      render: (h: number) => h > 0 ? `${h} ч` : '—',
    },
  ];

  const equipmentColumns = [
    { title: '# ', key: 'rank', render: (_: any, __: any, idx: number) => idx + 1 },
    { title: 'Оборудование',    dataIndex: 'equipmentName',    key: 'name' },
    { title: 'Серийный номер',  dataIndex: 'serialNumber',     key: 'serial', render: (v: string | null) => v ?? '—' },
    { title: 'Объект',          dataIndex: 'serviceObjectName', key: 'object' },
    { title: 'Заявок',          dataIndex: 'ticketCount',      key: 'count', sorter: (a: any, b: any) => a.ticketCount - b.ticketCount },
  ];

  const objectsColumns = [
    { title: 'Объект',        dataIndex: 'serviceObjectName', key: 'name' },
    { title: 'Клиент',        dataIndex: 'clientName',        key: 'client' },
    { title: 'Всего заявок',  dataIndex: 'totalTickets',      key: 'total', sorter: (a: any, b: any) => a.totalTickets - b.totalTickets },
    { title: 'Открытых',      dataIndex: 'openTickets',       key: 'open',  sorter: (a: any, b: any) => a.openTickets - b.openTickets },
  ];

  const priorityColumns = [
    {
      title: 'Приоритет',
      dataIndex: 'priority',
      key: 'priority',
      render: (p: number) => <Tag color={priorityLabels[p]?.color}>{priorityLabels[p]?.label}</Tag>,
    },
    {
      title: 'Среднее время решения',
      dataIndex: 'avgHours',
      key: 'avg',
      render: (h: number) => `${h} ч`,
    },
    { title: 'Заявок', dataIndex: 'count', key: 'count' },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Аналитика и отчёты</Title>
      </div>

      {}
      <Card style={{ marginBottom: 24 }}>
        <div className="filter-bar" style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
          <div>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Период</Text>
            <RangePicker
              value={range}
              onChange={(v) => v && setRange(v as [Dayjs, Dayjs])}
              format="DD.MM.YYYY"
              allowClear={false}
              presets={[
                { label: 'Последние 7 дней',  value: [dayjs().subtract(7, 'day'), dayjs()] },
                { label: 'Последние 30 дней', value: [dayjs().subtract(30, 'day'), dayjs()] },
                { label: 'Последние 3 мес.',  value: [dayjs().subtract(3, 'month'), dayjs()] },
                { label: 'Текущий год',       value: [dayjs().startOf('year'), dayjs()] },
              ]}
            />
          </div>
          <div>
            <Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Клиент</Text>
            <Select
              style={{ width: 220 }}
              placeholder="Все клиенты"
              allowClear
              showSearch
              optionFilterProp="children"
              onDropdownVisibleChange={(open) => open && loadClients()}
              onChange={(val) => setSelectedClient(val)}
            >
              {clients.map((c: any) => (
                <Select.Option key={c.id} value={c.id}>{c.name}</Select.Option>
              ))}
            </Select>
          </div>
          <div style={{ paddingTop: 22 }}>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              onClick={fetchAll}
              size="middle"
            >
              Сформировать
            </Button>
          </div>
        </div>
      </Card>

      {!loaded && (
        <Card>
          <div style={{ textAlign: 'center', padding: '48px 0', color: '#888' }}>
            <BarChartOutlined style={{ fontSize: 48, marginBottom: 16 }} />
            <p>Выберите период и нажмите «Сформировать» для загрузки отчётов</p>
          </div>
        </Card>
      )}

      {loaded && (
        <>
          {}
          <Card
            title={<Space><BarChartOutlined />SLA-показатели</Space>}
            style={{ marginBottom: 24 }}
          >
            {slaLoading ? <Spin /> : slaData ? (
              <Row gutter={24}>
                <Col xs={24} sm={12} md={6}>
                  <Statistic title="Ответ в срок" value={slaData.slaResponseRatePct} suffix="%" precision={1} />
                  <Progress
                    percent={slaData.slaResponseRatePct}
                    showInfo={false}
                    strokeColor={slaData.slaResponseRatePct >= 90 ? '#52c41a' : slaData.slaResponseRatePct >= 70 ? '#faad14' : '#ff4d4f'}
                    style={{ marginTop: 8 }}
                  />
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Statistic title="Решение в срок" value={slaData.slaResolutionRatePct} suffix="%" precision={1} />
                  <Progress
                    percent={slaData.slaResolutionRatePct}
                    showInfo={false}
                    strokeColor={slaData.slaResolutionRatePct >= 90 ? '#52c41a' : slaData.slaResolutionRatePct >= 70 ? '#faad14' : '#ff4d4f'}
                    style={{ marginTop: 8 }}
                  />
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Statistic
                    title="MTTR (среднее время решения)"
                    value={slaData.mttrHours}
                    suffix="ч"
                    precision={1}
                    prefix={<ClockCircleOutlined />}
                  />
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Statistic
                    title="Нарушений SLA"
                    value={slaData.slaBreached}
                    valueStyle={{ color: slaData.slaBreached > 0 ? '#ff4d4f' : '#52c41a' }}
                  />
                  <Text type="secondary">из {slaData.totalClosed} закрытых</Text>
                </Col>
              </Row>
            ) : <Text type="secondary">Нет данных</Text>}
          </Card>

          {}
          <Card
            title={<Space><TeamOutlined />Нагрузка инженеров</Space>}
            style={{ marginBottom: 24 }}
          >
            <Table
              columns={workloadColumns}
              dataSource={workloadData}
              loading={workloadLoading}
              rowKey="engineerId"
              pagination={false}
              size="small"
              locale={{ emptyText: 'Нет данных за период' }}
            />
          </Card>

          <Row gutter={24} style={{ marginBottom: 24 }}>
            {}
            <Col xs={24} lg={12}>
              <Card title={<Space><ToolOutlined />Проблемное оборудование (Топ-10)</Space>} style={{ height: '100%' }}>
                <Table
                  columns={equipmentColumns}
                  dataSource={equipmentData}
                  loading={equipmentLoading}
                  rowKey="equipmentId"
                  pagination={false}
                  size="small"
                  locale={{ emptyText: 'Нет данных' }}
                />
              </Card>
            </Col>

            {}
            <Col xs={24} lg={12}>
              <Card title={<Space><ClockCircleOutlined />Среднее время решения по приоритету</Space>} style={{ height: '100%' }}>
                <Table
                  columns={priorityColumns}
                  dataSource={priorityData}
                  loading={priorityLoading}
                  rowKey="priority"
                  pagination={false}
                  size="small"
                  locale={{ emptyText: 'Нет данных за период' }}
                />
              </Card>
            </Col>
          </Row>

          {}
          <Card title={<Space><EnvironmentOutlined />Заявки по объектам обслуживания</Space>}>
            <Table
              columns={objectsColumns}
              dataSource={objectsData}
              loading={objectsLoading}
              rowKey="serviceObjectId"
              pagination={{ pageSize: 10 }}
              size="small"
              locale={{ emptyText: 'Нет данных' }}
            />
          </Card>
        </>
      )}
    </div>
  );
};

export default ReportsPage;
